using System.Drawing;

namespace ListaCompras.UI.Themes
{
    public class ThemeSettingsModel
    {
        public bool IsDarkMode { get; set; }
        public Color Primary { get; set; } = Color.FromArgb(0, 120, 215);
        public Color PrimaryLight { get; set; } = Color.FromArgb(51, 153, 255);
        public Color PrimaryDark { get; set; } = Color.FromArgb(0, 90, 158);
        public Color Surface { get; set; } = Color.White;
        public Color Border { get; set; } = Color.FromArgb(224, 224, 224);
        public Color BorderFocus { get; set; } = Color.FromArgb(0, 120, 215);
        public Color TextPrimary { get; set; } = Color.Black;
        public Color TextSecondary { get; set; } = Color.Gray;
        public Color TextDisabled { get; set; } = Color.FromArgb(128, 128, 128);
        
        public Font BaseFont { get; set; } = new Font("Segoe UI", 9f);
        public float FontSize { get; set; } = 9f;
    }
}