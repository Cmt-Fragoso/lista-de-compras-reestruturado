using System;
using System.Drawing;
using System.Windows.Forms;
using ListaCompras.UI.Themes;

namespace ListaCompras.UI.Controls
{
    public class BaseButton : Button
    {
        private bool _isHovered;
        private bool _isPressed;

        public BaseButton()
        {
            InitializeButton();
            SubscribeToTheme();
        }

        private void InitializeButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = ThemeManager.Instance.GetFont();
            Cursor = Cursors.Hand;
            Padding = new Padding(16, 8, 16, 8);
            MinimumSize = new Size(80, 32);

            // Add event handlers
            MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _isHovered = false; _isPressed = false; Invalidate(); };
            MouseDown += (s, e) => { _isPressed = true; Invalidate(); };
            MouseUp += (s, e) => { _isPressed = false; Invalidate(); };
        }

        private void SubscribeToTheme()
        {
            ThemeManager.Instance.ThemeChanged += (s, e) => ApplyTheme();
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            BackColor = ThemeManager.Instance.CurrentTheme.Primary;
            ForeColor = Color.White;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var colors = ThemeManager.Instance.CurrentTheme;
            Color baseColor = Enabled ? colors.Primary : colors.TextDisabled;

            if (_isPressed && Enabled)
                BackColor = colors.PrimaryDark;
            else if (_isHovered && Enabled)
                BackColor = colors.PrimaryLight;
            else
                BackColor = baseColor;

            base.OnPaint(pevent);
        }
    }
}