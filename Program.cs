using System;
using System.Windows.Forms;
using ListaCompras.UI.Forms;

namespace ListaCompras
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}