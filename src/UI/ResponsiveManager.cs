using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using ListaCompras.UI.Forms;

namespace ListaCompras.UI
{
    public class ResponsiveManager
    {
        private static ResponsiveManager _instance;
        public static ResponsiveManager Instance => _instance ??= new ResponsiveManager();

        private readonly Dictionary<Control, ResponsiveInfo> _controlsInfo = new();
        private readonly List<Form> _registeredForms = new();
        private float _dpiScale = 1.0f;

        public class ResponsiveInfo
        {
            public float OriginalFontSize { get; set; }
            public Padding OriginalPadding { get; set; }
            public Size OriginalSize { get; set; }
            public Point OriginalLocation { get; set; }
            public AnchorStyles OriginalAnchor { get; set; }
            public bool UseRelativePositioning { get; set; }
            public float RelativeX { get; set; }
            public float RelativeY { get; set; }
            public float RelativeWidth { get; set; }
            public float RelativeHeight { get; set; }
        }

        private ResponsiveManager()
        {
            // Detecta DPI inicial
            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                _dpiScale = g.DpiX / 96f;
            }
        }

        public void RegisterForm(Form form)
        {
            if (_registeredForms.Contains(form)) return;

            _registeredForms.Add(form);
            form.ResizeBegin += Form_ResizeBegin;
            form.ResizeEnd += Form_ResizeEnd;
            form.SizeChanged += Form_SizeChanged;
            
            // Registra controles recursivamente
            RegisterControlsRecursively(form);
            
            // Configura handlers de DPI
            form.HandleCreated += (s, e) =>
            {
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    NativeMethods.SetProcessDPIAware();
                }
            };
        }

        private void RegisterControlsRecursively(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (!_controlsInfo.ContainsKey(control))
                {
                    var info = new ResponsiveInfo
                    {
                        OriginalFontSize = control.Font.Size,
                        OriginalPadding = control.Padding,
                        OriginalSize = control.Size,
                        OriginalLocation = control.Location,
                        OriginalAnchor = control.Anchor,
                        UseRelativePositioning = ShouldUseRelativePositioning(control),
                    };

                    if (info.UseRelativePositioning)
                    {
                        info.RelativeX = control.Left / (float)parent.ClientSize.Width;
                        info.RelativeY = control.Top / (float)parent.ClientSize.Height;
                        info.RelativeWidth = control.Width / (float)parent.ClientSize.Width;
                        info.RelativeHeight = control.Height / (float)parent.ClientSize.Height;
                    }

                    _controlsInfo[control] = info;
                }

                RegisterControlsRecursively(control);
            }
        }

        private bool ShouldUseRelativePositioning(Control control)
        {
            // Determina quais controles devem usar posicionamento relativo
            return control is Panel || 
                   control is GroupBox || 
                   control is TableLayoutPanel ||
                   control is FlowLayoutPanel ||
                   control.Dock == DockStyle.None;
        }

        private void Form_ResizeBegin(object sender, EventArgs e)
        {
            if (sender is Form form)
            {
                form.SuspendLayout();
            }
        }

        private void Form_ResizeEnd(object sender, EventArgs e)
        {
            if (sender is Form form)
            {
                form.ResumeLayout();
                form.PerformLayout();
            }
        }

        private void Form_SizeChanged(object sender, EventArgs e)
        {
            if (sender is not Form form) return;

            foreach (Control control in form.Controls)
            {
                AdjustControlRecursively(control);
            }
        }

        private void AdjustControlRecursively(Control control)
        {
            if (_controlsInfo.TryGetValue(control, out var info))
            {
                if (info.UseRelativePositioning && control.Parent != null)
                {
                    // Ajusta posição e tamanho relativos
                    int newX = (int)(info.RelativeX * control.Parent.ClientSize.Width);
                    int newY = (int)(info.RelativeY * control.Parent.ClientSize.Height);
                    int newWidth = (int)(info.RelativeWidth * control.Parent.ClientSize.Width);
                    int newHeight = (int)(info.RelativeHeight * control.Parent.ClientSize.Height);

                    if (control.Location != new Point(newX, newY))
                        control.Location = new Point(newX, newY);

                    if (control.Size != new Size(newWidth, newHeight))
                        control.Size = new Size(newWidth, newHeight);
                }

                // Ajusta fonte com base no DPI
                float scaledFontSize = info.OriginalFontSize * _dpiScale;
                if (Math.Abs(control.Font.Size - scaledFontSize) > 0.1f)
                {
                    control.Font = new Font(control.Font.FontFamily, scaledFontSize);
                }

                // Ajusta padding
                var scaledPadding = new Padding(
                    (int)(info.OriginalPadding.Left * _dpiScale),
                    (int)(info.OriginalPadding.Top * _dpiScale),
                    (int)(info.OriginalPadding.Right * _dpiScale),
                    (int)(info.OriginalPadding.Bottom * _dpiScale)
                );

                if (control.Padding != scaledPadding)
                {
                    control.Padding = scaledPadding;
                }
            }

            // Processa controles filhos
            foreach (Control child in control.Controls)
            {
                AdjustControlRecursively(child);
            }
        }

        public void UpdateDPIScale()
        {
            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                var newDpiScale = g.DpiX / 96f;
                if (Math.Abs(_dpiScale - newDpiScale) > 0.01f)
                {
                    _dpiScale = newDpiScale;
                    foreach (var form in _registeredForms)
                    {
                        foreach (Control control in form.Controls)
                        {
                            AdjustControlRecursively(control);
                        }
                    }
                }
            }
        }
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool SetProcessDPIAware();
    }
}
