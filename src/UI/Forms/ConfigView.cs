using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using ListaCompras.UI.Controls;
using ListaCompras.UI.Themes;
using ListaCompras.UI.Models;
using ListaCompras.UI.Services;

namespace ListaCompras.UI.Forms
{
    public class ConfigView : UserControl
    {
        private readonly IDataService _dataService;
        private ConfigModel _config;
        private bool _isLoading;

        private TabControl tabControl;
        private TabPage tabGeral;
        private TabPage tabVisualizacao;
        private TabPage tabBackup;
        private TabPage tabDados;
        private Label lblStatus;
        private BaseButton btnSalvar;
        private BaseButton btnRestaurarPadrao;

        #region Controles das Tabs
        // Geral
        private ComboBox cmbTema;
        private CheckBox chkUsarTemaDoSistema;
        private CheckBox chkSincronizarAutomaticamente;
        private NumericUpDown numIntervaloSincronizacao;
        private CheckBox chkNotificarAtualizacoes;

        // Visualização
        private CheckBox chkMostrarGraficos;
        private ComboBox cmbTipoGrafico;
        private NumericUpDown numPontosGrafico;
        private CheckBox chkMostrarMediaMovel;
        private CheckBox chkMostrarTendencia;
        private CheckBox chkMostrarEstatisticas;

        // Backup
        private CheckBox chkBackupAutomatico;
        private TextBox txtDiretorioBackup;
        private BaseButton btnSelecionarDiretorioBackup;
        private NumericUpDown numDiasManterBackup;

        // Dados
        private ComboBox cmbFormatoExportacao;
        private TextBox txtDiretorioExportacao;
        private BaseButton btnSelecionarDiretorioExportacao;
        private CheckBox chkIncluirEstatisticas;
        private CheckBox chkIncluirGraficos;
        #endregion

        #region Construtores e Inicialização
        public ConfigView(IDataService dataService)
        {
            _dataService = dataService;
            _config = ConfigModel.Default;
            InitializeComponent();
            ConfigureTheme();
            BindData();
        }

        [Código anterior de InitializeComponent() mantido...]

        #endregion

        #region Manipulação de Dados
        private void BindData()
        {
            _isLoading = true;
            try
            {
                // Geral
                cmbTema.Text = _config.Theme;
                chkUsarTemaDoSistema.Checked = _config.UsarTemaDoSistema;
                chkSincronizarAutomaticamente.Checked = _config.SincronizarAutomaticamente;
                numIntervaloSincronizacao.Value = _config.IntervaloSincronizacao;
                chkNotificarAtualizacoes.Checked = _config.NotificarAtualizacoes;

                // Visualização
                chkMostrarGraficos.Checked = _config.MostrarGraficos;
                cmbTipoGrafico.Text = _config.TipoGrafico;
                numPontosGrafico.Value = _config.QuantidadePontosGrafico;
                chkMostrarMediaMovel.Checked = _config.MostrarMediaMovel;
                chkMostrarTendencia.Checked = _config.MostrarTendencia;
                chkMostrarEstatisticas.Checked = _config.MostrarEstatisticas;

                // Backup
                chkBackupAutomatico.Checked = _config.BackupAutomatico;
                txtDiretorioBackup.Text = _config.DiretorioBackup;
                numDiasManterBackup.Value = _config.DiasManterBackup;

                // Dados
                cmbFormatoExportacao.Text = _config.FormatoExportacao;
                txtDiretorioExportacao.Text = _config.DiretorioExportacao;
                chkIncluirEstatisticas.Checked = _config.IncluirEstatisticas;
                chkIncluirGraficos.Checked = _config.IncluirGraficos;

                UpdateControlStates();
                lblStatus.Text = "Configurações carregadas";
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void UpdateConfig()
        {
            // Geral
            _config.Theme = cmbTema.Text;
            _config.UsarTemaDoSistema = chkUsarTemaDoSistema.Checked;
            _config.SincronizarAutomaticamente = chkSincronizarAutomaticamente.Checked;
            _config.IntervaloSincronizacao = (int)numIntervaloSincronizacao.Value;
            _config.NotificarAtualizacoes = chkNotificarAtualizacoes.Checked;

            // Visualização
            _config.MostrarGraficos = chkMostrarGraficos.Checked;
            _config.TipoGrafico = cmbTipoGrafico.Text;
            _config.QuantidadePontosGrafico = (int)numPontosGrafico.Value;
            _config.MostrarMediaMovel = chkMostrarMediaMovel.Checked;
            _config.MostrarTendencia = chkMostrarTendencia.Checked;
            _config.MostrarEstatisticas = chkMostrarEstatisticas.Checked;

            // Backup
            _config.BackupAutomatico = chkBackupAutomatico.Checked;
            _config.DiretorioBackup = txtDiretorioBackup.Text;
            _config.DiasManterBackup = (int)numDiasManterBackup.Value;

            // Dados
            _config.FormatoExportacao = cmbFormatoExportacao.Text;
            _config.DiretorioExportacao = txtDiretorioExportacao.Text;
            _config.IncluirEstatisticas = chkIncluirEstatisticas.Checked;
            _config.IncluirGraficos = chkIncluirGraficos.Checked;
        }

        private void UpdateControlStates()
        {
            numIntervaloSincronizacao.Enabled = chkSincronizarAutomaticamente.Checked;
            cmbTema.Enabled = !chkUsarTemaDoSistema.Checked;
            
            cmbTipoGrafico.Enabled = chkMostrarGraficos.Checked;
            numPontosGrafico.Enabled = chkMostrarGraficos.Checked;
            chkMostrarMediaMovel.Enabled = chkMostrarGraficos.Checked;
            chkMostrarTendencia.Enabled = chkMostrarGraficos.Checked;
            
            txtDiretorioBackup.Enabled = chkBackupAutomatico.Checked;
            btnSelecionarDiretorioBackup.Enabled = chkBackupAutomatico.Checked;
            numDiasManterBackup.Enabled = chkBackupAutomatico.Checked;

            chkIncluirGraficos.Enabled = chkMostrarGraficos.Checked;
        }
        #endregion

        #region Event Handlers
        private async void BtnSalvar_Click(object sender, EventArgs e)
        {
            try
            {
                lblStatus.Text = "Salvando configurações...";
                btnSalvar.Enabled = false;

                UpdateConfig();
                await _dataService.SaveConfigAsync(_config);

                // Aplica mudanças imediatas
                if (!chkUsarTemaDoSistema.Checked)
                {
                    ThemeManager.Instance.SetTheme(_config.Theme == "Dark" ? 
                        ThemeColors.Dark : ThemeColors.Default);
                }

                lblStatus.Text = "Configurações salvas com sucesso";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar configurações: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Erro ao salvar configurações";
            }
            finally
            {
                btnSalvar.Enabled = true;
            }
        }

        private void BtnRestaurarPadrao_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                "Deseja realmente restaurar todas as configurações para o padrão?",
                "Confirmar Restauração",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _config = ConfigModel.Default;
                BindData();
                lblStatus.Text = "Configurações restauradas para o padrão";
            }
        }

        private void BtnSelecionarDiretorioBackup_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Selecione o diretório para backup";
                dialog.UseDescriptionForTitle = true;
                
                if (!string.IsNullOrEmpty(txtDiretorioBackup.Text))
                    dialog.SelectedPath = txtDiretorioBackup.Text;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtDiretorioBackup.Text = dialog.SelectedPath;
                }
            }
        }

        private void BtnSelecionarDiretorioExportacao_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Selecione o diretório para exportação";
                dialog.UseDescriptionForTitle = true;
                
                if (!string.IsNullOrEmpty(txtDiretorioExportacao.Text))
                    dialog.SelectedPath = txtDiretorioExportacao.Text;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtDiretorioExportacao.Text = dialog.SelectedPath;
                }
            }
        }

        private void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
            {
                UpdateControlStates();
            }
        }
        #endregion

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            // Inscreve nos eventos após o binding inicial
            chkSincronizarAutomaticamente.CheckedChanged += CheckBox_CheckedChanged;
            chkUsarTemaDoSistema.CheckedChanged += CheckBox_CheckedChanged;
            chkMostrarGraficos.CheckedChanged += CheckBox_CheckedChanged;
            chkBackupAutomatico.CheckedChanged += CheckBox_CheckedChanged;
        }
    }
}