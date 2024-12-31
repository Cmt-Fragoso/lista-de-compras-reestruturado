using System;
using System.Drawing;
using System.Windows.Forms;
using ListaCompras.UI.Themes;

namespace ListaCompras.UI.Controls
{
    public class BaseTextBox : TextBox
    {
        private bool _isFocused;

        public BaseTextBox()
        {
            InitializeTextBox();
            SubscribeToTheme();
        }

        private void InitializeTextBox()
        {
            BorderStyle = BorderStyle.FixedSingle;
            Font = ThemeManager.Instance.GetFont();
            Padding = new Padding(8, 4, 8, 4);
            Size = new Size(200, 32);

            // Add event handlers
            GotFocus += (s, e) => { _isFocused = true; Invalidate(); };
            LostFocus += (s, e) => { _isFocused = false; Invalidate(); };
        }

        private void SubscribeToTheme()
        {
            ThemeManager.Instance.ThemeChanged += (s, e) => ApplyTheme();
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            var colors = ThemeManager.Instance.CurrentTheme;
            BackColor = colors.Surface;
            ForeColor = colors.TextPrimary;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_isFocused)
            {
                var colors = ThemeManager.Instance.CurrentTheme;
                using (var pen = new Pen(colors.BorderFocus, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }
        }
    }
}