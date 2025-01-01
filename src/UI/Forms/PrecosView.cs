// [Código anterior mantido...]

        private BaseButton btnExportar;
        private readonly ExportService _exportService;

        public PrecosView(IDataService dataService, ConfigModel config)
        {
            _dataService = dataService;
            _exportService = new ExportService(dataService, config);
            InitializeComponent();
            ConfigureTheme();
        }

        private void InitializeComponent()
        {
            // [Código anterior mantido até toolbarPanel]

            // Botão Exportar
            btnExportar = new BaseButton
            {
                Text = "Exportar",
                Width = 100,
                Dock = DockStyle.Right,
                Margin = new Padding(0, 0, 8, 0),
                Enabled = false
            };
            btnExportar.Click += BtnExportar_Click;

            // Layout
            toolbarPanel.Controls.AddRange(new Control[] {
                cmbCategoria,
                cmbItem,
                btnExportar,
                btnAtualizar,
                btnConfig
            });

            // [Resto do código mantido...]
        }

        private async void BtnExportar_Click(object sender, EventArgs e)
        {
            if (!(cmbItem.SelectedItem is ItemModel item))
            {
                MessageBox.Show("Selecione um item para exportar", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var dialog = new SaveFileDialog())
                {
                    string extensao = "";
                    string filtro = "";
                    
                    switch (_config.FormatoExportacao.ToLower())
                    {
                        case "excel":
                            extensao = ".xlsx";
                            filtro = "Arquivo Excel|*.xlsx";
                            break;
                        case "csv":
                            extensao = ".csv";
                            filtro = "Arquivo CSV|*.csv";
                            break;
                        case "pdf":
                            extensao = ".pdf";
                            filtro = "Arquivo PDF|*.pdf";
                            break;
                    }

                    dialog.FileName = $"Preços_{item.Nome}_{DateTime.Now:yyyyMMdd}{extensao}";
                    dialog.Filter = filtro;
                    dialog.InitialDirectory = string.IsNullOrEmpty(_config.DiretorioExportacao) ? 
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : 
                        _config.DiretorioExportacao;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        btnExportar.Enabled = false;
                        lblStatus.Text = "Exportando dados...";

                        await _exportService.ExportPrecosAsync(item.Id, dialog.FileName);

                        lblStatus.Text = "Dados exportados com sucesso!";
                        
                        if (MessageBox.Show(
                            "Dados exportados com sucesso! Deseja abrir o arquivo?",
                            "Exportação Concluída",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = dialog.FileName,
                                UseShellExecute = true
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar dados: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Erro ao exportar dados";
            }
            finally
            {
                btnExportar.Enabled = true;
            }
        }

        private async void CmbItem_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbItem.SelectedItem is ItemModel item)
            {
                await LoadPrecosAsync(item.Id);
                chart.Title = $"Histórico de Preços - {item.Nome}";
                btnExportar.Enabled = true;
            }
            else
            {
                gridPrecos.DataSource = null;
                UpdateChart();
                lblEstatisticas.Text = "";
                chart.Title = "Histórico de Preços";
                btnExportar.Enabled = false;
            }
        }

        // [Resto do código mantido...]
