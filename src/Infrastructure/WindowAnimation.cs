using System.Diagnostics;
using System.Runtime.InteropServices;

namespace winDrawer;

/// <summary>
/// Transition helpers shared by the two windows and the owner-drawn controls.
///
/// Windows are animated by writing <see cref="Form.Opacity"/> and <see cref="Control.Location"/> from a
/// single time-based timer; no composition/DWM interop is involved. Every transition is optional: when
/// Windows is configured not to show animations, <see cref="IsEnabled"/> returns false and the
/// transitions jump straight to their final state, which is exactly the behaviour the app had before
/// the transitions existed.
/// </summary>
internal static class Animations
{
    /// <summary><c>SPI_GETCLIENTAREAANIMATION</c>, the "Animation effects" switch in Windows settings.</summary>
    private const uint SpiGetClientAreaAnimation = 0x1042;

    private static int _frameIntervalMilliseconds = AppConstants.Defaults.AnimationFrameMillisecondsDefault;
    private static int _activeClocks;

    /// <summary>
    /// Frame interval configured by the user (<c>startup.animation_frame_ms</c>). Larger values give
    /// fewer, cheaper frames; 0 disables every transition.
    /// </summary>
    public static int FrameIntervalMilliseconds => _frameIntervalMilliseconds;

    /// <summary>Interval a running clock should use; 0 means transitions are disabled.</summary>
    public static int EffectiveFrameIntervalMilliseconds => _frameIntervalMilliseconds <= 0
        ? 0
        : Math.Max(AppConstants.Ui.Animation.MinFrameIntervalMilliseconds, _frameIntervalMilliseconds);

    /// <summary>Applies the configured frame interval. Non-positive values disable the transitions.</summary>
    public static void Configure(int frameIntervalMilliseconds)
    {
        var interval = Math.Clamp(
            frameIntervalMilliseconds,
            AppConstants.Defaults.AnimationFrameMillisecondsMin,
            AppConstants.Defaults.AnimationFrameMillisecondsMax);
        if (_frameIntervalMilliseconds == interval)
        {
            return;
        }

        _frameIntervalMilliseconds = interval;
        DrawerLogger.LogInformation(interval <= 0
            ? "Transition animations disabled by settings."
            : $"Transition frame interval set to {interval} ms.");
    }

    /// <summary>
    /// True when transitions should run: enabled in the settings and allowed by Windows. The system
    /// flag is queried per transition (the call is cheap) so a changed Windows setting applies without
    /// restarting the application.
    /// </summary>
    public static bool IsEnabled()
    {
        if (EffectiveFrameIntervalMilliseconds <= 0)
        {
            return false;
        }

        if (!SystemInformation.UIEffectsEnabled)
        {
            return false;
        }

        return !SystemParametersInfo(SpiGetClientAreaAnimation, 0, out var enabled, 0) || enabled;
    }

    /// <summary>
    /// Raises the system timer resolution while transitions run, so the requested frame interval is
    /// actually honoured instead of being rounded up to the default ~15.6 ms tick.
    /// </summary>
    public static void BeginFramePacing()
    {
        if (Interlocked.Increment(ref _activeClocks) != 1)
        {
            return;
        }

        if (timeBeginPeriod(AppConstants.Ui.Animation.TimerResolutionMilliseconds) != 0)
        {
            DrawerLogger.LogDebug("Unable to raise the timer resolution for transitions.");
        }
    }

    /// <summary>Drops the raised timer resolution once the last transition stopped.</summary>
    public static void EndFramePacing()
    {
        if (Interlocked.Decrement(ref _activeClocks) != 0)
        {
            return;
        }

        _ = timeEndPeriod(AppConstants.Ui.Animation.TimerResolutionMilliseconds);
    }

    /// <summary>Decelerating easing used by every transition.</summary>
    public static double EaseOutCubic(double progress)
    {
        var remaining = 1d - Math.Clamp(progress, 0d, 1d);
        return 1d - (remaining * remaining * remaining);
    }

    public static int Lerp(int from, int to, double progress)
    {
        return (int)Math.Round(from + ((to - from) * Math.Clamp(progress, 0d, 1d)));
    }

    public static double Lerp(double from, double to, double progress)
    {
        return from + ((to - from) * Math.Clamp(progress, 0d, 1d));
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(
        uint uiAction,
        uint uiParam,
        [MarshalAs(UnmanagedType.Bool)] out bool pvParam,
        uint fWinIni);

    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static extern uint timeBeginPeriod(uint period);

    [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    private static extern uint timeEndPeriod(uint period);
}

/// <summary>
/// Time-based animation driver around one WinForms timer. Progress comes from a
/// <see cref="Stopwatch"/>, so the duration does not depend on timer jitter, and the clock stops itself
/// as soon as the transition finished, was replaced or was cancelled.
/// </summary>
internal sealed class AnimationClock : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Stopwatch _stopwatch = new();
    private Action<double>? _onProgress;
    private Action? _onCompleted;
    private int _durationMilliseconds;
    private bool _framePacing;
    private bool _disposed;

    public AnimationClock()
    {
        _timer = new System.Windows.Forms.Timer();
        _timer.Tick += Timer_Tick;
    }

    public bool IsRunning => _timer.Enabled;

    /// <summary>
    /// Runs a transition. <paramref name="onProgress"/> receives eased progress in 0..1 and is called at
    /// least once with 1, also when animations are disabled.
    /// </summary>
    public void Start(int durationMilliseconds, Action<double> onProgress, Action? onCompleted = null)
    {
        ArgumentNullException.ThrowIfNull(onProgress);
        ObjectDisposedException.ThrowIf(_disposed, this);

        Detach();
        _onProgress = onProgress;
        _onCompleted = onCompleted;
        _durationMilliseconds = Math.Max(1, durationMilliseconds);

        if (!Animations.IsEnabled())
        {
            // 系统或设置关闭动画时直接落到终态，行为与没有过渡时一致。
            var completed = Detach();
            onProgress(1d);
            completed?.Invoke();
            return;
        }

        _timer.Interval = Animations.EffectiveFrameIntervalMilliseconds;
        if (!_framePacing)
        {
            _framePacing = true;
            Animations.BeginFramePacing();
        }

        onProgress(0d);
        _stopwatch.Restart();
        _timer.Start();
    }

    /// <summary>Stops the transition without completing it; the caller keeps the state it applied.</summary>
    public void Cancel() => Detach();

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_onProgress is null)
        {
            Detach();
            return;
        }

        var progress = Math.Clamp(_stopwatch.ElapsedMilliseconds / (double)_durationMilliseconds, 0d, 1d);
        _onProgress(Animations.EaseOutCubic(progress));
        if (progress < 1d)
        {
            return;
        }

        var completed = Detach();
        completed?.Invoke();
    }

    /// <summary>
    /// Stops the timer and hands back the completion callback. The callback is detached before it runs,
    /// so a transition that starts the next one cannot be cleared by its predecessor.
    /// </summary>
    private Action? Detach()
    {
        var completed = _onCompleted;
        _timer.Stop();
        _stopwatch.Reset();
        _onProgress = null;
        _onCompleted = null;
        _durationMilliseconds = 0;
        if (_framePacing)
        {
            _framePacing = false;
            Animations.EndFramePacing();
        }

        return completed;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Detach();
        _timer.Tick -= Timer_Tick;
        _timer.Dispose();
    }
}

/// <summary>
/// Fades a form between a fraction of its configured opacity and the configured opacity itself. The
/// configured value is the base; finishing or cancelling always writes the base back, so a transition
/// can never leave the window at a transparency the settings do not say.
/// </summary>
internal sealed class WindowFade : IDisposable
{
    private readonly Form _form;
    private readonly AnimationClock _clock = new();
    private double _baseOpacity;

    public WindowFade(Form form, double baseOpacity)
    {
        _form = form ?? throw new ArgumentNullException(nameof(form));
        _baseOpacity = Clamp(baseOpacity);
    }

    /// <summary>
    /// Opacity configured by the theme. Updating it while a transition runs keeps the current frame and
    /// continues towards the new base instead of snapping.
    /// </summary>
    public double BaseOpacity
    {
        get => _baseOpacity;
        set
        {
            _baseOpacity = Clamp(value);
            if (!_clock.IsRunning)
            {
                Apply(_baseOpacity);
            }
        }
    }

    /// <summary>Fades from <paramref name="fromFactor"/> of the base opacity up to the base opacity.</summary>
    public void FadeIn(int durationMilliseconds, double fromFactor, Action? onCompleted = null)
    {
        var start = Clamp(_baseOpacity * Math.Clamp(fromFactor, 0d, 1d));
        _clock.Start(
            durationMilliseconds,
            progress => Apply(Animations.Lerp(start, _baseOpacity, progress)),
            () =>
            {
                Apply(_baseOpacity);
                onCompleted?.Invoke();
            });
    }

    /// <summary>Stops a running fade and restores the configured opacity.</summary>
    public void Cancel()
    {
        _clock.Cancel();
        Apply(_baseOpacity);
    }

    private void Apply(double opacity)
    {
        if (_form.IsDisposed)
        {
            return;
        }

        var value = Clamp(opacity);
        if (Math.Abs(_form.Opacity - value) > 0.001d)
        {
            _form.Opacity = value;
        }
    }

    private static double Clamp(double opacity) => Math.Clamp(opacity, 0d, 1d);

    public void Dispose() => _clock.Dispose();
}

/// <summary>
/// Moves a form from its animated start position to the position the caller computed. The target is
/// always supplied by the caller (the main window owns drawer placement), so a cancelled transition
/// never leaves a stale offset behind.
/// </summary>
internal sealed class WindowSlide(Form form) : IDisposable
{
    private readonly Form _form = form ?? throw new ArgumentNullException(nameof(form));
    private readonly AnimationClock _clock = new();
    private Point _start;
    private Point _target;

    public bool IsRunning => _clock.IsRunning;

    public void Play(Point start, Point target, int durationMilliseconds)
    {
        _start = start;
        _target = target;

        if (!Animations.IsEnabled())
        {
            MoveTo(target);
            return;
        }

        _clock.Start(durationMilliseconds, MoveToProgress);
    }

    public void Cancel() => _clock.Cancel();

    private void MoveToProgress(double progress)
    {
        MoveTo(new Point(
            Animations.Lerp(_start.X, _target.X, progress),
            Animations.Lerp(_start.Y, _target.Y, progress)));
    }

    private void MoveTo(Point location)
    {
        if (_form.IsDisposed || _form.Location == location)
        {
            return;
        }

        _form.Location = location;
    }

    public void Dispose() => _clock.Dispose();
}

