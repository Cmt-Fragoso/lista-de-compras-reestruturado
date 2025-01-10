using System;
using System.Windows.Forms;
using ListaCompras.UI.Controls;
using ListaCompras.UI.Services;
using ListaCompras.Core.Models;
using ListaCompras.UI.Models;
using ListaCompras.UI.Themes;

namespace ListaCompras.UI.Forms
{
    public partial class PrecosView : UserControl
    {
        private readonly IDataService _dataService;
        private readonly ConfigModel _config;
        private BaseButton btnExportar;
        private readonly ExportService _exportService;

        public PrecosView(IDataService dataService, ConfigModel config)
        {
            _dataService = dataService;
            _config = config;
            _exportService = new ExportService();
            InitializeComponent();
            ConfigureTheme();
        }

        protected void InitializeComponent()
        {
            // Componentes
            btnExportar = new BaseButton
            {
                Text = "Exportar",
                Dock = DockStyle.Right,
                Margin = new Padding(4)
            };
            btnExportar.Click += BtnExportar_Click;

            Controls.Add(btnExportar);
        }

        protected void ConfigureTheme()
        {
            ThemeManager.Instance.ApplyTheme(this);
        }

        protected async void BtnExportar_Click(object sender, EventArgs e)
        {
            // Implementação
        }
    }
}