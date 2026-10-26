namespace winDrawer;

/// <summary>
/// Appearance transitions for <see cref="DrawerWindow"/>: the drawer slides out from the main window
/// edge and fades in. The final position always comes from <see cref="MainWindow"/>, so these methods
/// only add a visual offset that is dropped again when the transition is cancelled.
/// </summary>
public partial class DrawerWindow
{
    private WindowFade? _opacityTransition;
    private WindowSlide? _slideTransition;
    private Point? _appearanceTarget;
    private int _appearanceSlideOffset;

    /// <summary>
    /// Applies the animated start state (a little left of the final position and more transparent than
    /// the configured opacity). Called before <see cref="Form.Show()"/> so the drawer never flashes at
    /// its final position first. A <paramref name="slideOffsetPixels"/> of 0 appears in place.
    /// </summary>
    internal void PrepareAppearanceTransition(int slideOffsetPixels)
    {
        _appearanceTarget = Location;
        _appearanceSlideOffset = Math.Max(0, slideOffsetPixels);

        if (!Animations.IsEnabled())
        {
            return;
        }

        if (_appearanceSlideOffset > 0)
        {
            Location = new Point(_appearanceTarget.Value.X - _appearanceSlideOffset, _appearanceTarget.Value.Y);
        }

        ApplyOpacity(_theme.WindowOpacity * AppConstants.Ui.Animation.FadeInStartFactor);
    }

    /// <summary>Starts the slide/fade prepared by <see cref="PrepareAppearanceTransition"/>.</summary>
    internal void StartAppearanceTransition()
    {
        if (_appearanceTarget is not { } target)
        {
            return;
        }

        var slideOffset = _appearanceSlideOffset;
        _appearanceTarget = null;
        _appearanceSlideOffset = 0;

        var duration = slideOffset > 0
            ? AppConstants.Ui.Animation.NormalDurationMilliseconds
            : AppConstants.Ui.Animation.ShortDurationMilliseconds;

        if (slideOffset > 0)
        {
            SlideTransition.Play(Location, target, duration);
        }

        // A fade can toggle the layered window style; repainting the reclaimed top strip once keeps it
        // from showing the frame colour (see DrawerWindow.Chrome.cs).
        FadeTransition.FadeIn(duration, AppConstants.Ui.Animation.FadeInStartFactor, RefreshTopResizeGrip);
    }

    /// <summary>
    /// Stops a running appearance transition and restores the configured opacity. The position is left
    /// untouched: the caller that cancels (main-window drag, hide, close) applies the real position.
    /// </summary>
    internal void CancelAppearanceTransition()
    {
        _appearanceTarget = null;
        _appearanceSlideOffset = 0;
        _slideTransition?.Cancel();
        _opacityTransition?.Cancel();
    }

    private WindowFade FadeTransition => _opacityTransition ??= new WindowFade(this, _theme.WindowOpacity);

    private WindowSlide SlideTransition => _slideTransition ??= new WindowSlide(this);

    /// <summary>Theme opacity updates the base only, so a running fade continues at the new base.</summary>
    private void ApplyBaseOpacity(double opacity)
    {
        if (_opacityTransition is null)
        {
            ApplyOpacity(opacity);
            return;
        }

        _opacityTransition.BaseOpacity = opacity;
    }

    private void ApplyOpacity(double opacity)
    {
        if (IsDisposed)
        {
            return;
        }

        var value = Math.Clamp(opacity, 0d, 1d);
        if (Math.Abs(Opacity - value) > 0.001d)
        {
            Opacity = value;
        }
    }

    private void InitializeTransitions()
    {
        Disposed += (_, _) => ReleaseTransitions();
    }

    private void ReleaseTransitions()
    {
        _slideTransition?.Dispose();
        _slideTransition = null;
        _opacityTransition?.Dispose();
        _opacityTransition = null;
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible)
        {
            CancelAppearanceTransition();
        }
    }
}
