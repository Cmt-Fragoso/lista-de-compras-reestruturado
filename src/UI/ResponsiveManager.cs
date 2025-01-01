using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ListaCompras.UI
{
    public class ResponsiveManager
    {
        private static readonly Lazy<ResponsiveManager> _instance =
            new Lazy<ResponsiveManager>(() => new ResponsiveManager());
        
        public static ResponsiveManager Instance => _instance.Value;

        private readonly Dictionary<Form, FormLayoutInfo> _formLayouts;
        private readonly Timer _resizeTimer;
        private float _currentDpiScale = 1.0f;
        private Size _baseResolution = new Size(1920, 1080);
        private const int MinWidth = 800;
        private const int MinHeight = 600;

        private ResponsiveManager()
        {
            _formLayouts = new Dictionary<Form, FormLayoutInfo>();
            _resizeTimer = new Timer { Interval = 100 };
            _resizeTimer.Tick += HandleDelayedResize;
        }

        private class FormLayoutInfo
        {
            public Size InitialSize { get; set; }
            public Dictionary<Control, Rectangle> ControlLayouts { get; }
            public Dictionary<Control, Rectangle> NewLayouts { get; }
            public Dictionary<Control, float> InitialFontSizes { get; }
            public bool IsResizing { get; set; }
            public float MinScaleFactor { get; set; } = 0.7f;
            public float MaxScaleFactor { get; set; } = 1.5f;

            public FormLayoutInfo(Form form)
            {
                InitialSize = form.Size;
                ControlLayouts = new Dictionary<Control, Rectangle>();
                NewLayouts = new Dictionary<Control, Rectangle>();
                InitialFontSizes = new Dictionary<Control, float>();
            }

            public void CaptureInitialLayout()
            {
                CaptureControlLayout(ControlLayouts.Keys);
            }

            private void CaptureControlLayout(IEnumerable<Control> controls)
            {
                foreach (Control control in controls)
                {
                    if (!ControlLayouts.ContainsKey(control))
                    {
                        ControlLayouts[control] = control.Bounds;
                        if (control.Font != null)
                            InitialFontSizes[control] = control.Font.Size;
                    }
                }
            }
        }

        public void RegisterForm(Form form)
        {
            if (_formLayouts.ContainsKey(form)) return;

            var layoutInfo = new FormLayoutInfo(form);
            _formLayouts[form] = layoutInfo;

            form.ResizeBegin += (s, e) => BeginFormResize(form);
            form.ResizeEnd += (s, e) => EndFormResize(form);
            form.SizeChanged += (s, e) => HandleFormResize(form);
            form.Load += (s, e) => InitializeFormLayout(form);
            form.FormClosing += (s, e) => UnregisterForm(form);
            form.DpiChanged += (s, e) => HandleDpiChange(form, e.DeviceDpiOld, e.DeviceDpiNew);

            RegisterControlsRecursively(form, layoutInfo);
        }

        private void RegisterControlsRecursively(Control parent, FormLayoutInfo layoutInfo)
        {
            foreach (Control control in parent.Controls)
            {
                layoutInfo.ControlLayouts[control] = control.Bounds;
                if (control.Font != null)
                    layoutInfo.InitialFontSizes[control] = control.Font.Size;

                control.ControlAdded += (s, e) => RegisterControlsRecursively(e.Control, layoutInfo);
                RegisterControlsRecursively(control, layoutInfo);
            }
        }

        private void UnregisterForm(Form form)
        {
            if (_formLayouts.ContainsKey(form))
                _formLayouts.Remove(form);
        }

        private void SetMinimumSize(Form form)
        {
            form.MinimumSize = new Size(MinWidth, MinHeight);
        }

        private void BeginFormResize(Form form)
        {
            if (!_formLayouts.TryGetValue(form, out var layoutInfo)) return;

            layoutInfo.IsResizing = true;
            form.SuspendLayout();
        }

        private void EndFormResize(Form form)
        {
            if (!_formLayouts.TryGetValue(form, out var layoutInfo)) return;

            layoutInfo.IsResizing = false;
            form.ResumeLayout(true);
            
            _resizeTimer.Stop();
            HandleFormResize(form);
        }

        private void HandleFormResize(Form form)
        {
            if (!_formLayouts.TryGetValue(form, out var layoutInfo) || layoutInfo.IsResizing)
                return;

            _resizeTimer.Stop();
            _resizeTimer.Start();
        }

        private async void HandleDelayedResize(object sender, EventArgs e)
        {
            _resizeTimer.Stop();

            foreach (var form in new List<Form>(_formLayouts.Keys))
            {
                if (!form.IsDisposed && form.Visible)
                {
                    await Task.Run(() => CalculateNewLayout(form));
                    if (!form.IsDisposed)
                        form.BeginInvoke(new Action(() => ApplyNewLayout(form)));
                }
            }
        }

        private void CalculateNewLayout(Form form)
        {
            if (!_formLayouts.TryGetValue(form, out var layoutInfo)) return;

            var scaleX = (float)form.Width / layoutInfo.InitialSize.Width;
            var scaleY = (float)form.Height / layoutInfo.InitialSize.Height;
            var scale = Math.Min(scaleX, scaleY);

            // Limita o fator de escala
            scale = Math.Max(layoutInfo.MinScaleFactor, 
                   Math.Min(layoutInfo.MaxScaleFactor, scale));

            foreach (var control in layoutInfo.ControlLayouts)
            {
                var initial = control.Value;
                
                // Calcula nova posição mantendo proporcionalidade
                var newX = (int)(initial.X * scaleX);
                var newY = (int)(initial.Y * scaleY);
                var newWidth = (int)(initial.Width * scale);
                var newHeight = (int)(initial.Height * scale);

                // Ajusta para DPI
                newWidth = (int)(newWidth * _currentDpiScale);
                newHeight = (int)(newHeight * _currentDpiScale);

                var newBounds = new Rectangle(newX, newY, newWidth, newHeight);
                layoutInfo.NewLayouts[control.Key] = newBounds;
            }
        }

        private void ApplyNewLayout(Form form)
        {
            if (!_formLayouts.TryGetValue(form, out var layoutInfo)) return;

            form.SuspendLayout();

            try
            {
                foreach (var control in layoutInfo.NewLayouts)
                {
                    if (!control.Key.IsDisposed)
                    {
                        control.Key.Bounds = control.Value;
                        AdjustFontSize(control.Key, layoutInfo);
                        AdjustControlSpecifics(control.Key);
                    }
                }
            }
            finally
            {
                form.ResumeLayout(true);
            }
        }

        private void AdjustFontSize(Control control, FormLayoutInfo layoutInfo)
        {
            if (!layoutInfo.InitialFontSizes.TryGetValue(control, out float initialSize))
                return;

            var scale = Math.Min(
                (float)control.Width / layoutInfo.ControlLayouts[control].Width,
                (float)control.Height / layoutInfo.ControlLayouts[control].Height);

            scale = Math.Max(layoutInfo.MinScaleFactor,
                   Math.Min(layoutInfo.MaxScaleFactor, scale));

            float newSize = initialSize * scale * _currentDpiScale;
            
            if (control.Font.Size != newSize)
            {
                control.Font = new Font(control.Font.FontFamily, newSize, control.Font.Style);
            }
        }

        private void AdjustControlSpecifics(Control control)
        {
            if (control is DataGridView grid)
            {
                foreach (DataGridViewColumn column in grid.Columns)
                {
                    column.Width = (int)(column.Width * _currentDpiScale);
                }
            }
            else if (control is ListView listView)
            {
                foreach (ColumnHeader column in listView.Columns)
                {
                    column.Width = (int)(column.Width * _currentDpiScale);
                }
            }
            // Adicione outros controles específicos aqui
        }

        private void HandleDpiChange(Form form, int oldDpi, int newDpi)
        {
            float scaleFactor = newDpi / (float)oldDpi;
            _currentDpiScale = newDpi / 96f;

            if (!_formLayouts.TryGetValue(form, out var layoutInfo)) return;

            form.SuspendLayout();
            try
            {
                foreach (var control in layoutInfo.ControlLayouts.Keys)
                {
                    if (!control.IsDisposed)
                    {
                        var bounds = layoutInfo.ControlLayouts[control];
                        var newBounds = new Rectangle(
                            (int)(bounds.X * scaleFactor),
                            (int)(bounds.Y * scaleFactor),
                            (int)(bounds.Width * scaleFactor),
                            (int)(bounds.Height * scaleFactor));

                        control.Bounds = newBounds;
                        AdjustFontSize(control, layoutInfo);
                        AdjustControlSpecifics(control);
                    }
                }
            }
            finally
            {
                form.ResumeLayout(true);
            }
        }

        public void SetBaseResolution(Size resolution)
        {
            _baseResolution = resolution;
            foreach (var form in _formLayouts.Keys)
            {
                if (!form.IsDisposed)
                    HandleFormResize(form);
            }
        }

        public void SetScaleLimits(Form form, float minScale, float maxScale)
        {
            if (_formLayouts.TryGetValue(form, out var layoutInfo))
            {
                layoutInfo.MinScaleFactor = minScale;
                layoutInfo.MaxScaleFactor = maxScale;
                HandleFormResize(form);
            }
        }
    }
}