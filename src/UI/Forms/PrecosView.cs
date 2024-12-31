using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Linq;
using ListaCompras.UI.Controls;
using ListaCompras.UI.Themes;
using ListaCompras.UI.ViewModels;
using ListaCompras.UI.Services;
using ListaCompras.Core.Models;
using ListaCompras.UI.Models;

namespace ListaCompras.UI.Forms
{
    public class PrecosView : UserControl
    {
        private readonly IDataService _dataService;
        private Panel toolbarPanel;
        private Panel chartPanel;
        private Panel contentPanel;
        private BaseComboBox cmbCategoria;
        private BaseComboBox cmbItem;
        private BaseButton btnAtualizar;
        private BaseDataGrid gridPrecos;
        private BaseChart chart;
        private Panel configPanel;
        private BaseButton btnConfig;
        private ComboBox cmbTipoGrafico;
        private CheckBox chkMediaMovel;
        private CheckBox chkTendencia;
        private Label lblStatus;
        private Label lblEstatisticas;

        private BindingList<CategoriaModel> _categorias;
        private BindingList<ItemModel> _itens;
        private BindingList<PrecoModel> _precos;
        private ConfigModel _config;

        public PrecosView(IDataService dataService)
        {
            _dataService = dataService;
            _config = ConfigModel.Default;
            InitializeComponent();
            ConfigureTheme();
        }

        private void InitializeComponent()
        {
            // Initialize components
            toolbarPanel = new Panel();
            chartPanel = new Panel();
            contentPanel = new Panel();
            configPanel = new Panel();
            cmbCategoria = new BaseComboBox();
            cmbItem = new BaseComboBox();
            btnAtualizar = new BaseButton();
            btnConfig = new BaseButton();
            gridPrecos = new BaseDataGrid();
            chart = new BaseChart();
            cmbTipoGrafico = new ComboBox();
            chkMediaMovel = new CheckBox();
            chkTendencia = new CheckBox();
            lblStatus = new Label();
            lblEstatisticas = new Label();

            // Toolbar Panel
            toolbarPanel.Dock = DockStyle.Top;
            toolbarPanel.Height = 60;
            toolbarPanel.Padding = new Padding(16, 8, 16, 8);

            // Chart Panel
            chartPanel.Dock = DockStyle.Top;
            chartPanel.Height = 300;
            chartPanel.Padding = new Padding(16, 8, 16, 8);

            // Content Panel
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Padding = new Padding(16);

            // Config Panel
            configPanel.Dock = DockStyle.Right;
            configPanel.Width = 200;
            configPanel.Padding = new Padding(8);

            // Status Label
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 24;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Text = "Carregando...";

            // Estatísticas Label
            lblEstatisticas.Dock = DockStyle.Bottom;
            lblEstatisticas.Height = 24;
            lblEstatisticas.TextAlign = ContentAlignment.MiddleRight;
            lblEstatisticas.Text = "";

            // Categoria ComboBox
            cmbCategoria.Width = 200;
            cmbCategoria.Dock = DockStyle.Left;
            cmbCategoria.PlaceholderText = "Selecione a categoria...";
            cmbCategoria.SelectedIndexChanged += CmbCategoria_SelectedIndexChanged;

            // Item ComboBox
            cmbItem.Width = 300;
            cmbItem.Dock = DockStyle.Left;
            cmbItem.PlaceholderText = "Selecione o item...";
            cmbItem.Margin = new Padding(8, 0, 0, 0);
            cmbItem.SelectedIndexChanged += CmbItem_SelectedIndexChanged;

            // Atualizar Button
            btnAtualizar.Text = "Atualizar";
            btnAtualizar.Width = 100;
            btnAtualizar.Dock = DockStyle.Right;
            btnAtualizar.Click += BtnAtualizar_Click;

            // Config Button
            btnConfig.Text = "Configurações";
            btnConfig.Width = 120;
            btnConfig.Dock = DockStyle.Right;
            btnConfig.Margin = new Padding(0, 0, 8, 0);
            btnConfig.Click += BtnConfig_Click;

            // Chart
            chart.Dock = DockStyle.Fill;
            chart.ShowGrid = true;
            chart.ShowTooltips = true;
            chart.GridLines = 5;
            chart.Title = "Histórico de Preços";
            chart.XAxisLabel = "Data";
            chart.YAxisLabel = "Preço (R$)";

            // Tipo Gráfico ComboBox
            cmbTipoGrafico.Dock = DockStyle.Top;
            cmbTipoGrafico.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTipoGrafico.Items.AddRange(new[] { "Linha", "Barra" });
            cmbTipoGrafico.SelectedIndex = 0;
            cmbTipoGrafico.SelectedIndexChanged += CmbTipoGrafico_SelectedIndexChanged;

            // Checkboxes
            chkMediaMovel.Text = "Mostrar Média";
            chkMediaMovel.Dock = DockStyle.Top;
            chkMediaMovel.Checked = _config.MostrarMediaMovel;
            chkMediaMovel.CheckedChanged += ChkMediaMovel_CheckedChanged;

            chkTendencia.Text = "Mostrar Tendência";
            chkTendencia.Dock = DockStyle.Top;
            chkTendencia.Checked = _config.MostrarTendencia;
            chkTendencia.CheckedChanged += ChkTendencia_CheckedChanged;

            // Grid
            ConfigureGrid();

            // Layout
            toolbarPanel.Controls.AddRange(new Control[] {
                cmbCategoria,
                cmbItem,
                btnAtualizar,
                btnConfig
            });

            chartPanel.Controls.Add(chart);

            configPanel.Controls.AddRange(new Control[] {
                cmbTipoGrafico,
                chkMediaMovel,
                chkTendencia
            });

            contentPanel.Controls.AddRange(new Control[] {
                gridPrecos
            });

            Controls.AddRange(new Control[] {
                lblEstatisticas,
                lblStatus,
                contentPanel,
                chartPanel,
                toolbarPanel
            });

            // Configure base properties
            Dock = DockStyle.Fill;
        }

        private void ConfigureGrid()
        {
            gridPrecos.Dock = DockStyle.Fill;

            gridPrecos.AddColumn("Data", "Data", "Data", 150, 
                DataGridViewContentAlignment.MiddleCenter);
            gridPrecos.AddColumn("Valor", "Valor", "Valor", 120, 
                DataGridViewContentAlignment.MiddleRight);
            gridPrecos.AddColumn("Local", "Local", "Local", 200);
            gridPrecos.AddColumn("Observacao", "Observação", "Observacao", 300);

            gridPrecos.CellFormatting += GridPrecos_CellFormatting;
        }

        private void ConfigureTheme()
        {
            ThemeManager.Instance.ThemeChanged += (s, e) => ThemeManager.Instance.ApplyTheme(this);
            ThemeManager.Instance.ApplyTheme(this);
        }

        private void UpdateChart()
        {
            if (_precos == null || _precos.Count == 0)
            {
                chart.SetData(new List<double>(), new List<string>());
                return;
            }

            var data = _precos.Select(p => (double)p.Valor).ToList();
            var labels = _precos.Select(p => p.Data.ToString("dd/MM/yyyy")).ToList();

            chart.ChartType = cmbTipoGrafico.Text;
            chart.ShowAverage = chkMediaMovel.Checked;
            chart.ShowTrend = chkTendencia.Checked;
            chart.SetData(data, labels);
        }

        protected async override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            await LoadCategoriasAsync();
        }

        private async Task LoadCategoriasAsync()
        {
            try
            {
                cmbCategoria.IsLoading = true;
                lblStatus.Text = "Carregando categorias...";

                var categorias = await _dataService.GetCategoriasAsync();
                _categorias = new BindingList<CategoriaModel>(categorias);
                
                cmbCategoria.LoadItems(_categorias, "Nome", "Id");
                lblStatus.Text = "Categorias carregadas";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar categorias: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Erro ao carregar categorias";
            }
        }

        private async Task LoadItensAsync(int categoriaId)
        {
            try
            {
                cmbItem.IsLoading = true;
                lblStatus.Text = "Carregando itens...";

                var itens = await _dataService.GetItensByCategoriaAsync(categoriaId);
                _itens = new BindingList<ItemModel>(itens);
                
                cmbItem.LoadItems(_itens, "Nome", "Id");
                lblStatus.Text = "Itens carregados";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar itens: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Erro ao carregar itens";
            }
        }

        private async Task LoadPrecosAsync(int itemId)
        {
            try
            {
                gridPrecos.IsLoading = true;
                chart.IsLoading = true;
                lblStatus.Text = "Carregando preços...";

                var precos = await _dataService.GetPrecosAsync(itemId);
                _precos = new BindingList<PrecoModel>(precos);
                
                gridPrecos.LoadData(_precos);
                UpdateChart();
                AtualizarEstatisticas();
                
                lblStatus.Text = "Preços carregados";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar preços: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Erro ao carregar preços";
            }
            finally
            {
                gridPrecos.IsLoading = false;
                chart.IsLoading = false;
            }
        }

        private void AtualizarEstatisticas()
        {
            if (_precos == null || _precos.Count == 0)
            {
                lblEstatisticas.Text = "";
                return;
            }

            var precos = _precos.Select(p => p.Valor).ToList();
            var menor = precos.Min();
            var maior = precos.Max();
            var media = precos.Average();

            lblEstatisticas.Text = $"Menor: {menor:C2} | Maior: {maior:C2} | Média: {media:C2}";
        }

        #region Event Handlers
        private async void CmbCategoria_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbCategoria.SelectedItem is CategoriaModel categoria)
            {
                await LoadItensAsync(categoria.Id);
            }
            else
            {
                cmbItem.DataSource = null;
                gridPrecos.DataSource = null;
                UpdateChart();
                lblEstatisticas.Text = "";
            }
        }

        private async void CmbItem_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbItem.SelectedItem is ItemModel item)
            {
                await LoadPrecosAsync(item.Id);
                chart.Title = $"Histórico de Preços - {item.Nome}";
            }
            else
            {
                gridPrecos.DataSource = null;
                UpdateChart();
                lblEstatisticas.Text = "";
                chart.Title = "Histórico de Preços";
            }
        }

        private void CmbTipoGrafico_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateChart();
        }

        private void ChkMediaMovel_CheckedChanged(object sender, EventArgs e)
        {
            UpdateChart();
        }

        private void ChkTendencia_CheckedChanged(object sender, EventArgs e)
        {
            UpdateChart();
        }

        private async void BtnAtualizar_Click(object sender, EventArgs e)
        {
            if (cmbItem.SelectedItem is ItemModel item)
            {
                await LoadPrecosAsync(item.Id);
            }
        }

        private void BtnConfig_Click(object sender, EventArgs e)
        {
            // Toggle config panel
            if (configPanel.Parent == null)
            {
                Controls.Add(configPanel);
            }
            else
            {
                Controls.Remove(configPanel);
            }
        }

        private void GridPrecos_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null) return;

            var columnName = gridPrecos.Columns[e.ColumnIndex].Name;
            if (columnName == "Data" && e.Value is DateTime data)
            {
                e.Value = data.ToString("dd/MM/yyyy HH:mm");
                e.FormattingApplied = true;
            }
            else if (columnName == "Valor" && e.Value is decimal valor)
            {
                e.Value = valor.ToString("C2");
                e.FormattingApplied = true;
            }
        }
        #endregion
    }
}