using System.Drawing;

namespace ListaCompras.UI.Themes
{
    public class ThemeColors
    {
        // Primary Colors
        public Color Primary { get; set; }
        public Color PrimaryLight { get; set; }
        public Color PrimaryDark { get; set; }

        // Secondary Colors
        public Color Secondary { get; set; }
        public Color SecondaryLight { get; set; }
        public Color SecondaryDark { get; set; }

        // Background Colors
        public Color Background { get; set; }
        public Color BackgroundAlt { get; set; }
        public Color Surface { get; set; }

        // Text Colors
        public Color TextPrimary { get; set; }
        public Color TextSecondary { get; set; }
        public Color TextDisabled { get; set; }

        // Status Colors
        public Color Success { get; set; }
        public Color Warning { get; set; }
        public Color Error { get; set; }
        public Color Info { get; set; }

        // Border Colors
        public Color Border { get; set; }
        public Color BorderLight { get; set; }
        public Color BorderFocus { get; set; }

        public static ThemeColors Default => new ThemeColors
        {
            // Modern Windows-style colors
            Primary = Color.FromArgb(0, 120, 215),
            PrimaryLight = Color.FromArgb(51, 153, 255),
            PrimaryDark = Color.FromArgb(0, 90, 158),

            Secondary = Color.FromArgb(96, 205, 255),
            SecondaryLight = Color.FromArgb(140, 233, 255),
            SecondaryDark = Color.FromArgb(0, 153, 204),

            Background = Color.White,
            BackgroundAlt = Color.FromArgb(245, 245, 245),
            Surface = Color.FromArgb(250, 250, 250),

            TextPrimary = Color.FromArgb(51, 51, 51),
            TextSecondary = Color.FromArgb(102, 102, 102),
            TextDisabled = Color.FromArgb(153, 153, 153),

            Success = Color.FromArgb(46, 160, 67),
            Warning = Color.FromArgb(255, 153, 0),
            Error = Color.FromArgb(215, 58, 73),
            Info = Color.FromArgb(88, 166, 255),

            Border = Color.FromArgb(224, 224, 224),
            BorderLight = Color.FromArgb(240, 240, 240),
            BorderFocus = Color.FromArgb(0, 120, 215)
        };

        public static ThemeColors Dark => new ThemeColors
        {
            // Dark theme colors
            Primary = Color.FromArgb(0, 120, 215),
            PrimaryLight = Color.FromArgb(51, 153, 255),
            PrimaryDark = Color.FromArgb(0, 90, 158),

            Secondary = Color.FromArgb(96, 205, 255),
            SecondaryLight = Color.FromArgb(140, 233, 255),
            SecondaryDark = Color.FromArgb(0, 153, 204),

            Background = Color.FromArgb(32, 32, 32),
            BackgroundAlt = Color.FromArgb(43, 43, 43),
            Surface = Color.FromArgb(50, 50, 50),

            TextPrimary = Color.FromArgb(250, 250, 250),
            TextSecondary = Color.FromArgb(200, 200, 200),
            TextDisabled = Color.FromArgb(153, 153, 153),

            Success = Color.FromArgb(46, 160, 67),
            Warning = Color.FromArgb(255, 153, 0),
            Error = Color.FromArgb(215, 58, 73),
            Info = Color.FromArgb(88, 166, 255),

            Border = Color.FromArgb(70, 70, 70),
            BorderLight = Color.FromArgb(85, 85, 85),
            BorderFocus = Color.FromArgb(0, 120, 215)
        };
    }
}