using System;
using System.Windows.Forms;

namespace ListaCompras.UI
{
    public class ResponsiveManager : IDisposable
    {
        private readonly System.Windows.Forms.Timer _resizeTimer;
        private readonly Form _form;
        private Size _lastSize;

        public ResponsiveManager(Form form)
        {
            _form = form;
            _lastSize = form.Size;

            _resizeTimer = new System.Windows.Forms.Timer
            {
                Interval = 100
            };

            _resizeTimer.Tick += OnResizeTimerTick;
            _form.Resize += OnFormResize;
        }

        private void OnFormResize(object sender, EventArgs e)
        {
            _resizeTimer.Stop();
            _resizeTimer.Start();
        }

        private void OnResizeTimerTick(object sender, EventArgs e)
        {
            _resizeTimer.Stop();
            if (_form.Size != _lastSize)
            {
                _lastSize = _form.Size;
                AdjustLayout();
            }
        }

        private void AdjustLayout()
        {
            // Implementar ajustes de layout responsivo
        }

        public void Dispose()
        {
            _resizeTimer?.Dispose();
            _form.Resize -= OnFormResize;
        }
    }
}