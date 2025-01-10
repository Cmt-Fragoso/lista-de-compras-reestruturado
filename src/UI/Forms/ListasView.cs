using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Linq;
using ListaCompras.UI.Controls;
using ListaCompras.UI.Themes;
using ListaCompras.UI.ViewModels;
using ListaCompras.UI.Services;

namespace ListaCompras.UI.Forms
{
    public class ListasView : UserControl
    {
        private readonly IDataService _dataService;
        private Panel toolbarPanel;
        private Panel listPanel;
        private BaseButton btnNova;
        private BaseButton btnEditar;
        private BaseButton btnExcluir;
        private ListView listView;
        private BaseTextBox txtPesquisa;
        private Label lblStatus;

        public ListasView(IDataService dataService)
        {
            _dataService = dataService;
            InitializeComponent();
            ConfigureTheme();
        }

        private void InitializeComponent()
        {
            // Initialize components
            toolbarPanel = new Panel();
            listPanel = new Panel();
            btnNova = new BaseButton();
            btnEditar = new BaseButton();
            btnExcluir = new BaseButton();
            listView = new ListView();
            txtPesquisa = new BaseTextBox();
            lblStatus = new Label();

            // Toolbar Panel
            toolbarPanel.Dock = DockStyle.Top;
            toolbarPanel.Height = 60;
            toolbarPanel.Padding = new Padding(16, 8, 16, 8);

            // List Panel
            listPanel.Dock = DockStyle.Fill;
            listPanel.Padding = new Padding(16, 0, 16, 16);

            // Status Label
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 30;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Text = "Carregando...";

            // Search TextBox
            txtPesquisa.Dock = DockStyle.Left;
            txtPesquisa.Width = 300;
            txtPesquisa.PlaceholderText = "Pesquisar listas...";
            txtPesquisa.TextChanged += TxtPesquisa_TextChanged;

            // Buttons
            ConfigureButton(btnNova, "Nova Lista", Properties.Resources.IconAdd);
            ConfigureButton(btnEditar, "Editar", Properties.Resources.IconEdit);
            ConfigureButton(btnExcluir, "Excluir", Properties.Resources.IconDelete);
            
            btnNova.Click += BtnNova_Click;
            btnEditar.Click += BtnEditar_Click;
            btnExcluir.Click += BtnExcluir_Click;

            // ListView
            ConfigureListView();

            // Add controls to panels
            toolbarPanel.Controls.AddRange(new Control[] {
                txtPesquisa, btnNova, btnEditar, btnExcluir
            });

            listPanel.Controls.AddRange(new Control[] {
                lblStatus,
                listView
            });

            // Add panels to form
            Controls.AddRange(new Control[] {
                listPanel,
                toolbarPanel
            });

            // Configure base properties
            Dock = DockStyle.Fill;
        }

        private void ConfigureButton(BaseButton button, string text, Image icon)
        {
            button.Text = text;
            //button.Image = icon;
            button.ImageAlign = ContentAlignment.MiddleLeft;
            button.TextAlign = ContentAlignment.MiddleRight;
            button.Width = 120;
            button.Dock = DockStyle.Right;
            button.Margin = new Padding(8, 0, 0, 0);
        }

        private void ConfigureListView()
        {
            listView.Dock = DockStyle.Fill;
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.GridLines = true;
            listView.MultiSelect = false;

            // Add columns
            listView.Columns.Add("Nome", 300);
            listView.Columns.Add("Itens", 80);
            listView.Columns.Add("Valor Total", 120);
            listView.Columns.Add("Status", 100);
            listView.Columns.Add("Última Atualização", 150);

            // Configure events
            listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
            listView.DoubleClick += ListView_DoubleClick;
        }

        private void ConfigureTheme()
        {
            ThemeManager.Instance.ThemeChanged += (s, e) => {
                ThemeManager.Instance.ApplyTheme(this);
                UpdateListViewTheme();
            };
            ThemeManager.Instance.ApplyTheme(this);
            UpdateListViewTheme();
        }

        private void UpdateListViewTheme()
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            listView.BackColor = theme.Surface;
            listView.ForeColor = theme.TextPrimary;
        }

        public async Task LoadDataAsync()
        {
            try
            {
                lblStatus.Text = "Carregando listas...";
                listView.Items.Clear();
                btnEditar.Enabled = btnExcluir.Enabled = false;

                var listas = await _dataService.GetListasAsync();
                var viewModels = ViewModelFactory.CreateListaViewModels(listas);

                foreach (var lista in viewModels)
                {
                    AddListaToListView(lista);
                }

                lblStatus.Text = $"Total: {listas.Count} lista(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar listas: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Erro ao carregar listas";
            }
        }

        private void AddListaToListView(ListaViewModel lista)
        {
            var item = listView.Items.Add(lista.Nome);
            item.SubItems.AddRange(new[] {
                lista.QuantidadeItens.ToString(),
                lista.ValorTotal.ToString("C2"),
                lista.IsConcluida ? "Concluída" : "Em Andamento",
                lista.DataAtualizacao.ToString("dd/MM/yyyy HH:mm")
            });
            item.Tag = lista;
        }

        private async Task BuscarListasAsync(string termo)
        {
            try
            {
                var listas = await _dataService.GetListasAsync();
                var viewModels = ViewModelFactory.CreateListaViewModels(listas)
                    .Where(l => l.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                listView.Items.Clear();
                foreach (var lista in viewModels)
                {
                    AddListaToListView(lista);
                }

                lblStatus.Text = $"Encontradas: {viewModels.Count} lista(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao pesquisar listas: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Erro na pesquisa";
            }
        }

        #region Event Handlers

        private async void TxtPesquisa_TextChanged(object sender, EventArgs e)
        {
            var termo = txtPesquisa.Text.Trim();
            if (string.IsNullOrEmpty(termo))
                await LoadDataAsync();
            else
                await BuscarListasAsync(termo);
        }

        private async void BtnNova_Click(object sender, EventArgs e)
        {
            using (var form = new ListaEditForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var model = ViewModelFactory.CreateListaModel(form.Lista);
                        await _dataService.SaveListaAsync(model);
                        await LoadDataAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao salvar lista: {ex.Message}", "Erro",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void BtnEditar_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count == 0) return;

            var selectedItem = listView.SelectedItems[0];
            var lista = selectedItem.Tag as ListaViewModel;

            using (var form = new ListaEditForm(lista))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var model = ViewModelFactory.CreateListaModel(form.Lista);
                        await _dataService.SaveListaAsync(model);
                        await LoadDataAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao salvar lista: {ex.Message}", "Erro",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void BtnExcluir_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count == 0) return;

            var selectedItem = listView.SelectedItems[0];
            var lista = selectedItem.Tag as ListaViewModel;

            if (MessageBox.Show($"Deseja realmente excluir a lista '{lista.Nome}'?",
                "Confirmar Exclusão", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    await _dataService.DeleteListaAsync(lista.Id);
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao excluir lista: {ex.Message}", "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnEditar.Enabled = btnExcluir.Enabled = listView.SelectedItems.Count > 0;
        }

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 0)
            {
                BtnEditar_Click(sender, e);
            }
        }

        #endregion
    }
}