using System;
using System.Drawing;
using System.Windows.Forms;
using ListaCompras.UI.Controls;
using ListaCompras.UI.Themes;
using ListaCompras.Core.Models;
using ListaCompras.UI.Services;

namespace ListaCompras.UI.Forms
{
    public class PrecoEditForm : Form
    {
        private readonly IDataService _dataService;
        private readonly ItemModel _item;
        private PrecoModel _preco;

        private Panel toolbarPanel;
        private Panel contentPanel;
        private BaseButton btnSalvar;
        private BaseButton btnCancelar;
        private Label lblStatus;

        private Label lblItem;
        private Label lblItemValor;
        private Label lblData;
        private DateTimePicker dtpData;
        private Label lblValor;
        private BaseTextBox txtValor;
        private Label lblLocal;
        private BaseTextBox txtLocal;
        private Label lblObservacao;
        private TextBox txtObservacao;

        public PrecoEditForm(IDataService dataService, ItemModel item, PrecoModel preco = null)
        {
            _dataService = dataService;
            _item = item;
            _preco = preco ?? new PrecoModel 
            {
                ItemId = item.Id,
                Data = DateTime.Now
            };

            InitializeComponent();
            ConfigureTheme();
            BindData();
        }

        private void InitializeComponent()
        {
            // Initialize components
            toolbarPanel = new Panel();
            contentPanel = new Panel();
            btnSalvar = new BaseButton();
            btnCancelar = new BaseButton();
            lblStatus = new Label();

            lblItem = new Label();
            lblItemValor = new Label();
            lblData = new Label();
            dtpData = new DateTimePicker();
            lblValor = new Label();
            txtValor = new BaseTextBox();
            lblLocal = new Label();
            txtLocal = new BaseTextBox();
            lblObservacao = new Label();
            txtObservacao = new TextBox();

            // Form settings
            Text = _preco.Id == 0 ? "Novo Preço" : "Editar Preço";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(400, 400);
            MinimumSize = new Size(400, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Status Label
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 24;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Text = "";

            // Toolbar Panel
            toolbarPanel.Dock = DockStyle.Bottom;
            toolbarPanel.Height = 60;
            toolbarPanel.Padding = new Padding(16, 8, 16, 8);

            // Content Panel
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Padding = new Padding(16);

            // Configure Labels
            ConfigureLabel(lblItem, "Item:");
            lblItemValor.AutoSize = true;
            lblItemValor.Font = new Font(lblItemValor.Font, FontStyle.Bold);
            ConfigureLabel(lblData, "Data:");
            ConfigureLabel(lblValor, "Valor:");
            ConfigureLabel(lblLocal, "Local:");
            ConfigureLabel(lblObservacao, "Observação:");

            // Configure Controls
            dtpData.Format = DateTimePickerFormat.Custom;
            dtpData.CustomFormat = "dd/MM/yyyy HH:mm";
            dtpData.Width = 200;

            txtValor.Width = 150;
            txtValor.TextAlign = HorizontalAlignment.Right;
            txtValor.KeyPress += NumericOnly_KeyPress;

            txtLocal.Width = 300;
            txtLocal.MaxLength = 100;

            txtObservacao.Multiline = true;
            txtObservacao.Height = 60;
            txtObservacao.Width = 300;
            txtObservacao.ScrollBars = ScrollBars.Vertical;

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
                RowCount = 6,
                Padding = new Padding(0),
                ColumnStyles = {
                    new ColumnStyle(SizeType.AutoSize),
                    new ColumnStyle(SizeType.Percent, 100)
                }
            };

            tableLayout.Controls.Add(lblItem, 0, 0);
            tableLayout.Controls.Add(lblItemValor, 1, 0);
            
            tableLayout.Controls.Add(lblData, 0, 1);
            tableLayout.Controls.Add(dtpData, 1, 1);
            
            tableLayout.Controls.Add(lblValor, 0, 2);
            tableLayout.Controls.Add(txtValor, 1, 2);
            
            tableLayout.Controls.Add(lblLocal, 0, 3);
            tableLayout.Controls.Add(txtLocal, 1, 3);
            
            tableLayout.Controls.Add(lblObservacao, 0, 4);
            tableLayout.Controls.Add(txtObservacao, 1, 4);

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
            lblItemValor.Text = _item.Nome;
            dtpData.Value = _preco.Data;
            txtValor.Text = _preco.Valor.ToString("N2");
            txtLocal.Text = _preco.Local;
            txtObservacao.Text = _preco.Observacao;
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

        private async void BtnSalvar_Click(object sender, EventArgs e)
        {
            if (!ValidarDados()) return;

            try
            {
                lblStatus.Text = "Salvando...";
                btnSalvar.Enabled = false;

                _preco.Data = dtpData.Value;
                _preco.Valor = decimal.Parse(txtValor.Text);
                _preco.Local = txtLocal.Text;
                _preco.Observacao = txtObservacao.Text;

                await _dataService.SavePrecoAsync(_preco);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar preço: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Erro ao salvar";
                btnSalvar.Enabled = true;
            }
        }

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private bool ValidarDados()
        {
            if (!decimal.TryParse(txtValor.Text, out decimal valor) || valor <= 0)
            {
                MessageBox.Show("Digite um valor válido", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtValor.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtLocal.Text))
            {
                MessageBox.Show("Digite o local da compra", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLocal.Focus();
                return false;
            }

            return true;
        }
    }
}