using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using ListaCompras.UI.Controls;
using ListaCompras.UI.Themes;
using ListaCompras.UI.Services;
using ListaCompras.UI.Models;

namespace ListaCompras.UI.Forms
{
    public class BackupManagerForm : Form
    {
        private readonly BackupService _backupService;
        private readonly IDataService _dataService;
        private readonly ConfigModel _config;

        private Panel toolbarPanel;
        private Panel contentPanel;
        private Panel progressPanel;
        private BaseButton btnNovoBackup;
        private BaseButton btnRestaurar;
        private BaseButton btnFechar;
        private ListView listView;
        private ProgressBar progressBar;
        private Label lblStatus;

        public BackupManagerForm(IDataService dataService, ConfigModel config)
        {
            _dataService = dataService;
            _config = config;
            _backupService = new BackupService(dataService, config);
            InitializeComponent();
            ConfigureTheme();
            SubscribeToEvents();
            LoadBackups();
        }

        private void InitializeComponent()
        {
            // Initialize components
            toolbarPanel = new Panel();
            contentPanel = new Panel();
            progressPanel = new Panel();
            btnNovoBackup = new BaseButton();
            btnRestaurar = new BaseButton();
            btnFechar = new BaseButton();
            listView = new ListView();
            progressBar = new ProgressBar();
            lblStatus = new Label();

            // Form settings
            Text = "Gerenciador de Backup";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(800, 600);
            MinimumSize = new Size(600, 400);

            // Toolbar Panel
            toolbarPanel.Dock = DockStyle.Top;
            toolbarPanel.Height = 60;
            toolbarPanel.Padding = new Padding(16, 8, 16, 8);

            // Content Panel
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Padding = new Padding(16, 0, 16, 16);

            // Progress Panel
            progressPanel.Dock = DockStyle.Bottom;
            progressPanel.Height = 60;
            progressPanel.Padding = new Padding(16, 8, 16, 8);

            // Buttons
            btnNovoBackup.Text = "Novo Backup";
            btnNovoBackup.Width = 120;
            btnNovoBackup.Dock = DockStyle.Left;
            btnNovoBackup.Click += BtnNovoBackup_Click;

            btnRestaurar.Text = "Restaurar";
            btnRestaurar.Width = 100;
            btnRestaurar.Dock = DockStyle.Left;
            btnRestaurar.Margin = new Padding(8, 0, 0, 0);
            btnRestaurar.Enabled = false;
            btnRestaurar.Click += BtnRestaurar_Click;

            btnFechar.Text = "Fechar";
            btnFechar.Width = 100;
            btnFechar.Dock = DockStyle.Right;
            btnFechar.Click += BtnFechar_Click;

            // ListView
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.GridLines = true;
            listView.MultiSelect = false;
            listView.Dock = DockStyle.Fill;

            listView.Columns.Add("Nome", 200);
            listView.Columns.Add("Data", 150);
            listView.Columns.Add("Tamanho", 100);
            listView.Columns.Add("Status", 100);

            listView.SelectedIndexChanged += ListView_SelectedIndexChanged;

            // Progress Bar
            progressBar.Dock = DockStyle.Fill;
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Visible = false;

            // Status Label
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;

            // Layout
            toolbarPanel.Controls.AddRange(new Control[] {
                btnNovoBackup,
                btnRestaurar,
                btnFechar
            });

            progressPanel.Controls.AddRange(new Control[] {
                progressBar,
                lblStatus
            });

            contentPanel.Controls.Add(listView);

            Controls.AddRange(new Control[] {
                progressPanel,
                contentPanel,
                toolbarPanel
            });
        }

        private void ConfigureTheme()
        {
            ThemeManager.Instance.ThemeChanged += (s, e) => ThemeManager.Instance.ApplyTheme(this);
            ThemeManager.Instance.ApplyTheme(this);
        }

        private void SubscribeToEvents()
        {
            _backupService.BackupProgress += (s, message) =>
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => lblStatus.Text = message));
                }
                else
                {
                    lblStatus.Text = message;
                }
            };

            _backupService.BackupProgressPercentage += (s, progress) =>
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => UpdateProgress(progress.current, progress.total)));
                }
                else
                {
                    UpdateProgress(progress.current, progress.total);
                }
            };
        }

        private void UpdateProgress(int current, int total)
        {
            progressBar.Visible = true;
            progressBar.Minimum = 0;
            progressBar.Maximum = total;
            progressBar.Value = current;

            if (current >= total)
            {
                progressBar.Visible = false;
            }
        }

        private void LoadBackups()
        {
            listView.Items.Clear();

            if (string.IsNullOrEmpty(_config.DiretorioBackup) || !Directory.Exists(_config.DiretorioBackup))
                return;

            var directory = new DirectoryInfo(_config.DiretorioBackup);
            var files = directory.GetFiles("*.lcbk");

            foreach (var file in files.OrderByDescending(f => f.LastWriteTime))
            {
                var item = listView.Items.Add(file.Name);
                item.SubItems.Add(file.LastWriteTime.ToString("dd/MM/yyyy HH:mm"));
                item.SubItems.Add(FormatFileSize(file.Length));
                item.SubItems.Add("Disponível");
                item.Tag = file;
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

        private async void BtnNovoBackup_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Arquivo de Backup|*.lcbk";
                dialog.FileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.lcbk";
                
                if (!string.IsNullOrEmpty(_config.DiretorioBackup))
                {
                    dialog.InitialDirectory = _config.DiretorioBackup;
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        EnableControls(false);
                        await _backupService.CreateBackupAsync(dialog.FileName);
                        LoadBackups();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao criar backup: {ex.Message}", "Erro",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EnableControls(true);
                    }
                }
            }
        }

        private async void BtnRestaurar_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count == 0) return;

            var file = listView.SelectedItems[0].Tag as FileInfo;
            if (file == null) return;

            if (MessageBox.Show(
                "Deseja realmente restaurar este backup? Isso substituirá todos os dados atuais.",
                "Confirmar Restauração",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    EnableControls(false);
                    await _backupService.RestoreBackupAsync(file.FullName);
                    MessageBox.Show("Backup restaurado com sucesso!", "Sucesso",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao restaurar backup: {ex.Message}", "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    EnableControls(true);
                }
            }
        }

        private void BtnFechar_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void ListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnRestaurar.Enabled = listView.SelectedItems.Count > 0;
        }

        private void EnableControls(bool enabled)
        {
            btnNovoBackup.Enabled = enabled;
            btnRestaurar.Enabled = enabled && listView.SelectedItems.Count > 0;
            btnFechar.Enabled = enabled;
            listView.Enabled = enabled;
        }
    }
}