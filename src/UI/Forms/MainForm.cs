using System;
using System.Drawing;
using System.Windows.Forms;
using ListaCompras.UI.Controls;
using ListaCompras.UI.Themes;

namespace ListaCompras.UI.Forms
{
    public partial class MainForm : Form
    {
        private Panel sideMenu;
        private Panel contentPanel;
        private BaseButton btnListas;
        private BaseButton btnItens;
        private BaseButton btnPrecos;
        private BaseButton btnConfig;
        
        // Views
        private ListasView listasView;

        public MainForm()
        {
            InitializeComponent();
            SetupForm();
            ConfigureTheme();
        }

        private void InitializeComponent()
        {
            this.sideMenu = new Panel();
            this.contentPanel = new Panel();
            this.btnListas = new BaseButton();
            this.btnItens = new BaseButton();
            this.btnPrecos = new BaseButton();
            this.btnConfig = new BaseButton();

            // Initialize views
            this.listasView = new ListasView();

            // SideMenu
            this.sideMenu.Dock = DockStyle.Left;
            this.sideMenu.Width = 200;
            this.sideMenu.Padding = new Padding(8);

            // Content Panel
            this.contentPanel.Dock = DockStyle.Fill;
            this.contentPanel.Padding = new Padding(16);

            // Buttons
            ConfigureMenuButton(btnListas, "Listas de Compras", 0);
            ConfigureMenuButton(btnItens, "Itens", 1);
            ConfigureMenuButton(btnPrecos, "Preços", 2);
            ConfigureMenuButton(btnConfig, "Configurações", 3);

            // Add controls
            this.sideMenu.Controls.AddRange(new Control[] { 
                btnListas, btnItens, btnPrecos, btnConfig 
            });

            this.Controls.AddRange(new Control[] { 
                contentPanel, sideMenu 
            });
        }

        private void ConfigureMenuButton(BaseButton button, string text, int position)
        {
            button.Text = text;
            button.Dock = DockStyle.Top;
            button.Height = 40;
            button.FlatStyle = FlatStyle.Flat;
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Padding = new Padding(16, 0, 0, 0);
            button.Margin = new Padding(0, position == 0 ? 0 : 8, 0, 0);
            button.Click += MenuButton_Click;
        }

        private void SetupForm()
        {
            this.Text = "Lista de Compras";
            this.Size = new Size(1024, 768);
            this.MinimumSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void ConfigureTheme()
        {
            ThemeManager.Instance.ThemeChanged += (s, e) => ThemeManager.Instance.ApplyTheme(this);
            ThemeManager.Instance.ApplyTheme(this);
        }

        private void MenuButton_Click(object sender, EventArgs e)
        {
            var button = sender as BaseButton;
            if (button == null) return;

            // Clear current content
            contentPanel.Controls.Clear();

            // Load new content based on button
            switch (button.Text)
            {
                case "Listas de Compras":
                    LoadListasView();
                    break;
                case "Itens":
                    LoadItensView();
                    break;
                case "Preços":
                    LoadPrecosView();
                    break;
                case "Configurações":
                    LoadConfigView();
                    break;
            }

            // Update visual state
            foreach (Control ctrl in sideMenu.Controls)
            {
                if (ctrl is BaseButton btn)
                {
                    btn.BackColor = btn == button ? 
                        ThemeManager.Instance.CurrentTheme.PrimaryDark : 
                        ThemeManager.Instance.CurrentTheme.Primary;
                }
            }
        }

        private void LoadListasView()
        {
            contentPanel.Controls.Add(listasView);
            listasView.LoadData();
        }

        private void LoadItensView()
        {
            var label = new Label
            {
                Text = "Itens - Em desenvolvimento",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            contentPanel.Controls.Add(label);
        }

        private void LoadPrecosView()
        {
            var label = new Label
            {
                Text = "Preços - Em desenvolvimento",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            contentPanel.Controls.Add(label);
        }

        private void LoadConfigView()
        {
            var label = new Label
            {
                Text = "Configurações - Em desenvolvimento",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            contentPanel.Controls.Add(label);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            btnListas.PerformClick(); // Seleciona a view inicial
        }
    }
}