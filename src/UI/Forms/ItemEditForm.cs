using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using ListaCompras.UI.Controls;
using ListaCompras.UI.Themes;
using ListaCompras.UI.ViewModels;
using ListaCompras.UI.Services;
using ListaCompras.Core.Models;

namespace ListaCompras.UI.Forms
{
    public class ItemEditForm : Form
    {
        private readonly IDataService _dataService;
        private readonly ItemViewModel _viewModel;
        private List<CategoriaModel> _categorias;
        
        private Panel toolbarPanel;
        private Panel contentPanel;
        private BaseButton btnSalvar;
        private BaseButton btnCancelar;
        
        private Label lblNome;
        private BaseTextBox txtNome;
        
        private Label lblQuantidade;
        private BaseTextBox txtQuantidade;
        
        private Label lblUnidade;
        private ComboBox cmbUnidade;
        
        private Label lblPreco;
        private BaseTextBox txtPreco;
        
        private Label lblCategoria;
        private ComboBox cmbCategoria;
        private BaseButton btnNovaCategoria;
        
        private Label lblObservacao;
        private TextBox txtObservacao;
        
        private Label lblTotal;
        private Label lblStatus;

        public ItemEditForm(IDataService dataService, ItemViewModel viewModel = null)
        {
            _dataService = dataService;
            _viewModel = viewModel ?? new ItemViewModel();
            InitializeComponent();
            ConfigureTheme();
        }

        protected async override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            await LoadCategoriasAsync();
            BindData();
        }

        private void InitializeComponent()
        {
            // Initialize components
            toolbarPanel = new Panel();
            contentPanel = new Panel();
            btnSalvar = new BaseButton();
            btnCancelar = new BaseButton();
            
            lblNome = new Label();
            txtNome = new BaseTextBox();
            
            lblQuantidade = new Label();
            txtQuantidade = new BaseTextBox();
            
            lblUnidade = new Label();
            cmbUnidade = new ComboBox();
            
            lblPreco = new Label();
            txtPreco = new BaseTextBox();
            
            lblCategoria = new Label();
            cmbCategoria = new ComboBox();
            btnNovaCategoria = new BaseButton();
            
            lblObservacao = new Label();
            txtObservacao = new TextBox();
            
            lblTotal = new Label();
            lblStatus = new Label();

            // Form settings
            Text = _viewModel.Nome == null ? "Novo Item" : "Editar Item";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(500, 550);
            MinimumSize = new Size(400, 450);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Status Label
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 24;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;

            // Toolbar Panel
            toolbarPanel.Dock = DockStyle.Bottom;
            toolbarPanel.Height = 60;
            toolbarPanel.Padding = new Padding(16, 8, 16, 8);

            // Content Panel
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Padding = new Padding(16);

            // Configure Labels
            ConfigureLabel(lblNome, "Nome:");
            ConfigureLabel(lblQuantidade, "Quantidade:");
            ConfigureLabel(lblUnidade, "Unidade:");
            ConfigureLabel(lblPreco, "Preço:");
            ConfigureLabel(lblCategoria, "Categoria:");
            ConfigureLabel(lblObservacao, "Observação:");

            // Configure TextBoxes
            txtNome.Width = 350;
            txtNome.MaxLength = 100;
            
            txtQuantidade.Width = 100;
            txtQuantidade.TextAlign = HorizontalAlignment.Right;
            txtQuantidade.KeyPress += NumericOnly_KeyPress;
            txtQuantidade.TextChanged += Quantidade_TextChanged;
            
            txtPreco.Width = 100;
            txtPreco.TextAlign = HorizontalAlignment.Right;
            txtPreco.KeyPress += NumericOnly_KeyPress;
            txtPreco.TextChanged += Preco_TextChanged;

            // Configure ComboBoxes
            cmbUnidade.Width = 100;
            cmbUnidade.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbUnidade.Items.AddRange(ItemViewModel.UnidadesPadrao);
            
            cmbCategoria.Width = 250;
            cmbCategoria.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCategoria.Format += (s, e) => {
                if (e.ListItem != null)
                {
                    var categoria = e.ListItem as CategoriaModel;
                    e.Value = categoria.Nome;
                }
            };

            // Configure Nova Categoria Button
            btnNovaCategoria.Text = "Nova";
            btnNovaCategoria.Width = 80;
            btnNovaCategoria.Click += BtnNovaCategoria_Click;

            // Configure Observação
            txtObservacao.Multiline = true;
            txtObservacao.Height = 60;
            txtObservacao.ScrollBars = ScrollBars.Vertical;
            txtObservacao.Width = 350;

            // Total Label
            lblTotal.Text = "Total: R$ 0,00";
            lblTotal.AutoSize = true;
            lblTotal.Font = new Font(lblTotal.Font, FontStyle.Bold);

            // Buttons
            btnSalvar.Text = "Salvar";
            btnSalvar.Width = 100;
            btnSalvar.Dock = DockStyle.Right;
            btnSalvar.Click += BtnSalvar_Click;

            btnCancelar.Text = "Cancelar";
            btnCancelar.Width = 100;
            btnCancelar.Dock = DockStyle.Right;
            btnCancelar.Margin = new Padding(0, 0, 8, 0);
            btnCancelar.Click += BtnCancelar_Click;

            // Layout
            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(0),
                ColumnStyles = {
                    new ColumnStyle(SizeType.AutoSize),
                    new ColumnStyle(SizeType.Percent, 100)
                }
            };

            tableLayout.Controls.Add(lblNome, 0, 0);
            tableLayout.Controls.Add(txtNome, 1, 0);
            
            var quantidadePanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight
            };
            quantidadePanel.Controls.Add(txtQuantidade);
            quantidadePanel.Controls.Add(cmbUnidade);
            
            tableLayout.Controls.Add(lblQuantidade, 0, 1);
            tableLayout.Controls.Add(quantidadePanel, 1, 1);
            
            tableLayout.Controls.Add(lblPreco, 0, 2);
            tableLayout.Controls.Add(txtPreco, 1, 2);

            var categoriaPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight
            };
            categoriaPanel.Controls.Add(cmbCategoria);
            categoriaPanel.Controls.Add(btnNovaCategoria);
            
            tableLayout.Controls.Add(lblCategoria, 0, 3);
            tableLayout.Controls.Add(categoriaPanel, 1, 3);
            
            tableLayout.Controls.Add(lblObservacao, 0, 4);
            tableLayout.Controls.Add(txtObservacao, 1, 4);
            
            tableLayout.Controls.Add(new Panel(), 0, 5);
            tableLayout.Controls.Add(lblTotal, 1, 5);

            contentPanel.Controls.Add(tableLayout);

            toolbarPanel.Controls.AddRange(new Control[] {
                btnSalvar,
                btnCancelar
            });

            Controls.AddRange(new Control[] {
                lblStatus,
                contentPanel,
                toolbarPanel
            });
        }

        private async Task LoadCategoriasAsync()
        {
            try
            {
                lblStatus.Text = "Carregando categorias...";
                _categorias = await _dataService.GetCategoriasAsync();
                cmbCategoria.Items.Clear();
                cmbCategoria.Items.AddRange(_categorias.ToArray());

                if (_viewModel.CategoriaId.HasValue)
                {
                    var categoria = _categorias.Find(c => c.Id == _viewModel.CategoriaId.Value);
                    if (categoria != null)
                        cmbCategoria.SelectedItem = categoria;
                }

                lblStatus.Text = "";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Erro ao carregar categorias";
                MessageBox.Show($"Erro ao carregar categorias: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnNovaCategoria_Click(object sender, EventArgs e)
        {
            var nome = Microsoft.VisualBasic.Interaction.InputBox(
                "Digite o nome da nova categoria:", "Nova Categoria", "");

            if (!string.IsNullOrWhiteSpace(nome))
            {
                try
                {
                    var categoria = new CategoriaModel { Nome = nome };
                    categoria = await _dataService.SaveCategoriaAsync(categoria);
                    await LoadCategoriasAsync();
                    cmbCategoria.SelectedItem = _categorias.Find(c => c.Id == categoria.Id);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao criar categoria: {ex.Message}", "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ConfigureLabel(Label label, string text)
        {
            label.Text = text;
            label.AutoSize = true;
            label.Padding = new Padding(0, 6, 8, 0);
        }

        private void ConfigureTheme()
        {
            ThemeManager.Instance.ThemeChanged += (s, e) => ThemeManager.Instance.ApplyTheme(this);
            ThemeManager.Instance.ApplyTheme(this);
        }

        private void BindData()
        {
            if (_viewModel != null)
            {
                txtNome.Text = _viewModel.Nome;
                txtQuantidade.Text = _viewModel.Quantidade.ToString("N3");
                cmbUnidade.Text = _viewModel.Unidade;
                txtPreco.Text = _viewModel.Preco.ToString("N2");
                txtObservacao.Text = _viewModel.Observacao;
                AtualizarTotal();
            }
        }

        private void AtualizarTotal()
        {
            lblTotal.Text = $"Total: {_viewModel.Total:C2}";
        }

        private void NumericOnly_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            if ((e.KeyChar == ',' || e.KeyChar == '.') && ((TextBox)sender).Text.IndexOf(',') > -1)
            {
                e.Handled = true;
            }
        }

        private void Quantidade_TextChanged(object sender, EventArgs e)
        {
            if (decimal.TryParse(txtQuantidade.Text, out decimal quantidade))
            {
                _viewModel.Quantidade = quantidade;
                AtualizarTotal();
            }
        }

        private void Preco_TextChanged(object sender, EventArgs e)
        {
            if (decimal.TryParse(txtPreco.Text, out decimal preco))
            {
                _viewModel.Preco = preco;
                AtualizarTotal();
            }
        }

        private void BtnSalvar_Click(object sender, EventArgs e)
        {
            if (ValidarDados())
            {
                SalvarDados();
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private bool ValidarDados()
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show("Digite um nome para o item", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNome.Focus();
                return false;
            }

            if (!decimal.TryParse(txtQuantidade.Text, out decimal quantidade) || quantidade <= 0)
            {
                MessageBox.Show("Digite uma quantidade válida", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtQuantidade.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(cmbUnidade.Text))
            {
                MessageBox.Show("Selecione uma unidade", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbUnidade.Focus();
                return false;
            }

            if (!decimal.TryParse(txtPreco.Text, out decimal preco) || preco < 0)
            {
                MessageBox.Show("Digite um preço válido", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPreco.Focus();
                return false;
            }

            return true;
        }

        private void SalvarDados()
        {
            _viewModel.Nome = txtNome.Text;
            _viewModel.Quantidade = decimal.Parse(txtQuantidade.Text);
            _viewModel.Unidade = cmbUnidade.Text;
            _viewModel.Preco = decimal.Parse(txtPreco.Text);
            _viewModel.Observacao = txtObservacao.Text;
            
            if (cmbCategoria.SelectedItem is CategoriaModel categoria)
            {
                _viewModel.CategoriaId = categoria.Id;
                _viewModel.CategoriaNome = categoria.Nome;
            }
        }

        public ItemViewModel Item => _viewModel;
    }
}