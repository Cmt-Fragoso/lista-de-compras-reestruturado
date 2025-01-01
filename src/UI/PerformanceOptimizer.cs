using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace ListaCompras.UI
{
    public static class PerformanceOptimizer
    {
        private static readonly ConcurrentDictionary<Control, BufferedGraphics> _buffers = new ConcurrentDictionary<Control, BufferedGraphics>();
        private static readonly ConcurrentDictionary<Form, DateTime> _lastUpdates = new ConcurrentDictionary<Form, DateTime>();
        private static readonly TimeSpan UpdateThreshold = TimeSpan.FromMilliseconds(16); // ~60fps

        public static void OptimizeForm(Form form)
        {
            form.SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                         ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint, true);

            foreach (Control control in form.Controls)
            {
                OptimizeControl(control);
            }

            form.ControlAdded += (s, e) => OptimizeControl(e.Control);
            form.Disposed += (s, e) => CleanupForm(form);
        }

        private static void OptimizeControl(Control control)
        {
            if (control is DataGridView grid)
            {
                OptimizeDataGridView(grid);
            }
            else if (control is ListView list)
            {
                OptimizeListView(list);
            }
            else if (control is UserControl custom)
            {
                OptimizeUserControl(custom);
            }

            foreach (Control child in control.Controls)
            {
                OptimizeControl(child);
            }
        }

        private static void OptimizeDataGridView(DataGridView grid)
        {
            grid.DoubleBuffered = true;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.VirtualMode = grid.RowCount > 1000;
        }

        private static void OptimizeListView(ListView list)
        {
            list.DoubleBuffered = true;
            list.VirtualMode = list.Items.Count > 1000;
            if (list.VirtualMode)
            {
                list.VirtualListSize = list.Items.Count;
            }
        }

        private static void OptimizeUserControl(UserControl control)
        {
            control.SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                           ControlStyles.AllPaintingInWmPaint | 
                           ControlStyles.UserPaint, true);
            control.DoubleBuffered = true;
        }

        public static void SuspendDrawing(Control control)
        {
            Message msgSuspendUpdate = Message.Create(control.Handle, 0x0B, IntPtr.Zero, IntPtr.Zero);
            NativeWindow.FromHandle(control.Handle).DefWndProc(ref msgSuspendUpdate);
        }

        public static void ResumeDrawing(Control control)
        {
            Message msgResumeUpdate = Message.Create(control.Handle, 0x0B, IntPtr.Zero, IntPtr.Zero);
            NativeWindow.FromHandle(control.Handle).DefWndProc(ref msgResumeUpdate);
            control.Invalidate();
        }

        public static async Task UpdateControlAsync(Control control, Action updateAction)
        {
            if (control.InvokeRequired)
            {
                await Task.Run(() => {
                    control.Invoke(new Action(() => {
                        SuspendDrawing(control);
                        try
                        {
                            updateAction();
                        }
                        finally
                        {
                            ResumeDrawing(control);
                        }
                    }));
                });
            }
            else
            {
                SuspendDrawing(control);
                try
                {
                    updateAction();
                }
                finally
                {
                    ResumeDrawing(control);
                }
            }
        }

        public static BufferedGraphics GetBuffer(Control control)
        {
            return _buffers.GetOrAdd(control, c => {
                var context = BufferedGraphicsManager.Current;
                context.MaximumBuffer = new Size(c.Width + 1, c.Height + 1);
                return context.Allocate(c.CreateGraphics(), c.ClientRectangle);
            });
        }

        public static void CleanupForm(Form form)
        {
            _lastUpdates.TryRemove(form, out _);
            foreach (Control control in form.Controls)
            {
                if (_buffers.TryRemove(control, out var buffer))
                {
                    buffer.Dispose();
                }
            }
        }

        public static bool ShouldUpdate(Form form)
        {
            var now = DateTime.UtcNow;
            var lastUpdate = _lastUpdates.GetOrAdd(form, now);
            
            if (now - lastUpdate < UpdateThreshold)
                return false;

            _lastUpdates[form] = now;
            return true;
        }

        public static void OptimizeMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                NativeMethods.SetProcessWorkingSetSize(
                    System.Diagnostics.Process.GetCurrentProcess().Handle,
                    -1, -1);
            }
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("kernel32.dll")]
            public static extern bool SetProcessWorkingSetSize(IntPtr process, int minSize, int maxSize);
        }
    }
}