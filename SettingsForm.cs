using System;
using System.Drawing;
using System.Windows.Forms;

namespace winDrawer
{
    public partial class SettingsForm : Form
    {
        public Color SelectedAccentColor { get; private set; }
        public bool LaunchOnStartup => autoLaunchCheckBox.Checked;
        public bool ShutdownRequested { get; private set; }

        public SettingsForm()
        {
            InitializeComponent();
            DoubleBuffered = true;
            SelectedAccentColor = Color.Transparent;
            UpdatePreview();
        }

        private void ChannelTrackBar_Scroll(object? sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void okButton_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelButton_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void shutdownButton_Click(object? sender, EventArgs e)
        {
            ShutdownRequested = true;
            DialogResult = DialogResult.Abort;
            Close();
        }

        private void UpdatePreview()
        {
            var hue = hueTrackBar.Value;
            var saturation = saturationTrackBar.Value / 100f;
            var lightness = lightnessTrackBar.Value / 100f;
            var alpha = alphaTrackBar.Value / 100f;

            SelectedAccentColor = ColorFromHsla(hue, saturation, lightness, alpha);
            previewPanel.BackColor = SelectedAccentColor;
            previewLabel.Text = $"hsla({hue}, {saturationTrackBar.Value}%, {lightnessTrackBar.Value}%, {alpha:0.00})";
        }

        private static Color ColorFromHsla(int hue, float saturation, float lightness, float alpha)
        {
            // Conversion adapted from standard HSL to RGB formula.
            lightness = Math.Clamp(lightness, 0f, 1f);
            saturation = Math.Clamp(saturation, 0f, 1f);

            float q = lightness < 0.5f
                ? lightness * (1 + saturation)
                : lightness + saturation - lightness * saturation;
            float p = 2 * lightness - q;

            float r = HueToRgb(p, q, hue / 360f + 1f / 3f);
            float g = HueToRgb(p, q, hue / 360f);
            float b = HueToRgb(p, q, hue / 360f - 1f / 3f);

            var a = (int)Math.Clamp(alpha * 255f, 0f, 255f);
            return Color.FromArgb(a, (int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        private static float HueToRgb(float p, float q, float t)
        {
            if (t < 0f)
            {
                t += 1f;
            }
            if (t > 1f)
            {
                t -= 1f;
            }
            if (t < 1f / 6f)
            {
                return p + (q - p) * 6f * t;
            }
            if (t < 1f / 2f)
            {
                return q;
            }
            if (t < 2f / 3f)
            {
                return p + (q - p) * (2f / 3f - t) * 6f;
            }
            return p;
        }
    }
}

