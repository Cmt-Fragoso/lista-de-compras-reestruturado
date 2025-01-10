using System;
using System.Windows.Forms;
using System.Reflection;

namespace ListaCompras.UI
{
    public static class PerformanceOptimizer
    {
        public static void OptimizeControl(Control control)
        {
            if (control == null) return;

            // Usa reflection para acessar propriedades protegidas
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;

            // SetStyle
            var setStyleMethod = control.GetType().GetMethod("SetStyle", flags);
            if (setStyleMethod != null)
            {
                setStyleMethod.Invoke(control, new object[] 
                { 
                    ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.UserPaint, 
                    true 
                });
            }

            // DoubleBuffered
            var doubleBufferedProp = control.GetType().GetProperty("DoubleBuffered", flags);
            if (doubleBufferedProp != null)
            {
                doubleBufferedProp.SetValue(control, true);
            }

            // Recursivamente otimiza controles filhos
            foreach (Control child in control.Controls)
            {
                OptimizeControl(child);
            }
        }
    }
}