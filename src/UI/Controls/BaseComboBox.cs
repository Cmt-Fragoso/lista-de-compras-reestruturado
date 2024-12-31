using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using ListaCompras.UI.Themes;

namespace ListaCompras.UI.Controls
{
    public class BaseComboBox : ComboBox
    {
        private bool _isLoading;
        private string _loadingText = "Carregando...";
        private string _placeholderText = "";
        private bool _showPlaceholder = true;

        public BaseComboBox()
        {
            InitializeComboBox();
            SubscribeToTheme();
        }

        private void InitializeComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            Font = ThemeManager.Instance.GetFont();
        }

        private void SubscribeToTheme()
        {
            ThemeManager.Instance.ThemeChanged += (s, e) => ApplyTheme();
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            BackColor = theme.Surface;
            ForeColor = theme.TextPrimary;
            Invalidate();
        }

        [Browsable(true)]
        [Category("Appearance")]
        [DefaultValue("")]
        public string PlaceholderText
        {
            get => _placeholderText;
            set
            {
                if (_placeholderText != value)
                {
                    _placeholderText = value;
                    Invalidate();
                }
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [DefaultValue(true)]
        public bool ShowPlaceholder
        {
            get => _showPlaceholder;
            set
            {
                if (_showPlaceholder != value)
                {
                    _showPlaceholder = value;
                    Invalidate();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    Enabled = !value;
                    Invalidate();
                }
            }
        }

        public string LoadingText
        {
            get => _loadingText;
            set
            {
                if (_loadingText != value)
                {
                    _loadingText = value;
                    if (_isLoading) Invalidate();
                }
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();
            var theme = ThemeManager.Instance.CurrentTheme;

            var text = Items[e.Index].ToString();
            var textColor = e.State.HasFlag(DrawItemState.Selected) ?
                Color.White : theme.TextPrimary;

            using (var brush = new SolidBrush(textColor))
            {
                var bounds = e.Bounds;
                bounds.Inflate(-2, 0);
                e.Graphics.DrawString(text, Font, brush, bounds);
            }

            e.DrawFocusRectangle();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (SelectedIndex < 0 && _showPlaceholder && !string.IsNullOrEmpty(_placeholderText))
            {
                var theme = ThemeManager.Instance.CurrentTheme;
                using (var brush = new SolidBrush(theme.TextSecondary))
                {
                    var bounds = ClientRectangle;
                    bounds.Inflate(-2, 0);
                    e.Graphics.DrawString(_placeholderText, Font, brush, bounds);
                }
            }

            if (_isLoading)
            {
                var theme = ThemeManager.Instance.CurrentTheme;
                using (var brush = new SolidBrush(theme.TextSecondary))
                {
                    var bounds = ClientRectangle;
                    bounds.Inflate(-2, 0);
                    e.Graphics.DrawString(_loadingText, Font, brush, bounds);
                }
            }
        }

        public void LoadItems<T>(BindingList<T> items, string displayMember = null, string valueMember = null)
        {
            IsLoading = true;
            DataSource = null;

            try
            {
                if (!string.IsNullOrEmpty(displayMember))
                    DisplayMember = displayMember;

                if (!string.IsNullOrEmpty(valueMember))
                    ValueMember = valueMember;

                DataSource = items;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public new void BeginUpdate()
        {
            base.BeginUpdate();
            IsLoading = true;
        }

        public new void EndUpdate()
        {
            base.EndUpdate();
            IsLoading = false;
        }
    }
}