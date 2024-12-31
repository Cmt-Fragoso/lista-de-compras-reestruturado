using System;
using System.Drawing;
using System.Windows.Forms;

namespace ListaCompras.UI.Themes
{
    public class ThemeManager
    {
        private static ThemeManager _instance;
        private ThemeColors _currentTheme;
        public event EventHandler ThemeChanged;

        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ThemeManager();
                return _instance;
            }
        }

        private ThemeManager()
        {
            _currentTheme = ThemeColors.Default;
        }

        public ThemeColors CurrentTheme => _currentTheme;

        public void SetTheme(ThemeColors theme)
        {
            _currentTheme = theme;
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ApplyTheme(Control control)
        {
            if (control == null) return;

            // Apply theme to the control based on its type
            switch (control)
            {
                case Form form:
                    ApplyFormTheme(form);
                    break;
                case Button button:
                    ApplyButtonTheme(button);
                    break;
                case TextBox textBox:
                    ApplyTextBoxTheme(textBox);
                    break;
                case Label label:
                    ApplyLabelTheme(label);
                    break;
                case Panel panel:
                    ApplyPanelTheme(panel);
                    break;
            }

            // Recursively apply theme to child controls
            foreach (Control child in control.Controls)
            {
                ApplyTheme(child);
            }
        }

        private void ApplyFormTheme(Form form)
        {
            form.BackColor = _currentTheme.Background;
            form.ForeColor = _currentTheme.TextPrimary;
        }

        private void ApplyButtonTheme(Button button)
        {
            button.BackColor = _currentTheme.Primary;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = _currentTheme.Border;

            // Add hover effects
            button.MouseEnter += (s, e) => button.BackColor = _currentTheme.PrimaryLight;
            button.MouseLeave += (s, e) => button.BackColor = _currentTheme.Primary;
        }

        private void ApplyTextBoxTheme(TextBox textBox)
        {
            textBox.BackColor = _currentTheme.Surface;
            textBox.ForeColor = _currentTheme.TextPrimary;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        private void ApplyLabelTheme(Label label)
        {
            label.ForeColor = _currentTheme.TextPrimary;
            label.BackColor = Color.Transparent;
        }

        private void ApplyPanelTheme(Panel panel)
        {
            panel.BackColor = _currentTheme.BackgroundAlt;
            panel.ForeColor = _currentTheme.TextPrimary;
        }

        public Font GetFont(FontStyle style = FontStyle.Regular, float size = 9.0f)
        {
            return new Font("Segoe UI", size, style);
        }
    }
}