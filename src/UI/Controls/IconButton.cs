using System;
using System.Drawing;
using System.Windows.Forms;
using ListaCompras.UI.Resources;
using ListaCompras.UI.Themes;

namespace ListaCompras.UI.Controls
{
    public class IconButton : BaseButton
    {
        private string _iconName;
        private int _iconSize = 16;
        private bool _iconOnly = false;
        private ContentAlignment _iconAlignment = ContentAlignment.MiddleLeft;
        
        public string IconName
        {
            get => _iconName;
            set
            {
                _iconName = value;
                RefreshIcon();
            }
        }

        public int IconSize
        {
            get => _iconSize;
            set
            {
                _iconSize = value;
                RefreshIcon();
            }
        }

        public bool IconOnly
        {
            get => _iconOnly;
            set
            {
                _iconOnly = value;
                RecalculateLayout();
            }
        }

        public ContentAlignment IconAlignment
        {
            get => _iconAlignment;
            set
            {
                _iconAlignment = value;
                RecalculateLayout();
            }
        }

        public IconButton() : base()
        {
            ThemeManager.Instance.ThemeChanged += (s, e) => RefreshIcon();
        }

        private void RefreshIcon()
        {
            if (string.IsNullOrEmpty(_iconName))
            {
                Image = null;
                return;
            }

            try
            {
                Image = IconManager.Instance.GetIcon(_iconName, _iconSize);
                RecalculateLayout();
            }
            catch (ArgumentException)
            {
                Image = null;
            }
        }

        private void RecalculateLayout()
        {
            if (_iconOnly)
            {
                TextImageRelation = TextImageRelation.ImageBeforeText;
                ImageAlign = ContentAlignment.MiddleCenter;
                TextAlign = ContentAlignment.MiddleCenter;
                Text = string.Empty;
            }
            else
            {
                TextImageRelation = TextImageRelation.ImageBeforeText;
                ImageAlign = _iconAlignment;
                TextAlign = ContentAlignment.MiddleCenter;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (Image != null && Enabled)
            {
                // Adiciona efeito de hover no ícone
                if (ClientRectangle.Contains(PointToClient(MousePosition)))
                {
                    using (var overlay = new SolidBrush(Color.FromArgb(30, Color.White)))
                    {
                        e.Graphics.FillRectangle(overlay, ClientRectangle);
                    }
                }
            }
        }
    }
}