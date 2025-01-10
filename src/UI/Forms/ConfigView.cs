using System;
using System.Windows.Forms;
using ListaCompras.UI.Controls;
using ListaCompras.UI.Models;
using ListaCompras.UI.Services;

namespace ListaCompras.UI.Forms
{
    public partial class ConfigView : UserControl
    {
        protected CheckBox chkBackupAutomatico;
        protected TextBox txtDiretorioBackup;
        protected NumericUpDown numDiasManterBackup;
        protected BaseButton btnSelecionarDiretorioBackup;
        private readonly IDataService _dataService;
        private ConfigModel _config;
        private Label lblStatus;

        public ConfigView(IDataService dataService, ConfigModel config)
        {
            _dataService = dataService;
            _config = config;
            InitializeComponent();
            InitializeBackupTab();
            ConfigureTheme();
        }

        protected void InitializeBackupTab()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 3,
                RowCount = 4,
                AutoSize = true
            };

            // Resto do código mantido igual...
        }

        protected async void BtnGerenciarBackups_Click(object sender, EventArgs e)
        {
            // Código mantido igual...
        }
    }
}