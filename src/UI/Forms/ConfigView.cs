// [Código anterior mantido...]

        private void InitializeBackupTab()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 3,
                RowCount = 4,
                AutoSize = true
            };

            // Backup
            chkBackupAutomatico = new CheckBox
            {
                Text = "Backup automático",
                AutoSize = true
            };

            txtDiretorioBackup = new TextBox
            {
                Width = 300,
                ReadOnly = true
            };

            btnSelecionarDiretorioBackup = new BaseButton
            {
                Text = "...",
                Width = 30
            };
            btnSelecionarDiretorioBackup.Click += BtnSelecionarDiretorioBackup_Click;

            numDiasManterBackup = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 365,
                Value = 30,
                Width = 80
            };

            // Botão Gerenciar Backups
            var btnGerenciarBackups = new BaseButton
            {
                Text = "Gerenciar Backups",
                Width = 150,
                Margin = new Padding(0, 16, 0, 0)
            };
            btnGerenciarBackups.Click += BtnGerenciarBackups_Click;

            // Layout
            panel.Controls.Add(chkBackupAutomatico, 1, 0);
            panel.Controls.Add(new Label { Text = "Diretório:", AutoSize = true }, 0, 1);
            panel.Controls.Add(txtDiretorioBackup, 1, 1);
            panel.Controls.Add(btnSelecionarDiretorioBackup, 2, 1);
            AddFormField(panel, "Manter por (dias):", numDiasManterBackup, 2);
            panel.Controls.Add(btnGerenciarBackups, 1, 3);

            tabBackup.Controls.Add(panel);
        }

        private async void BtnGerenciarBackups_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_config.DiretorioBackup))
            {
                MessageBox.Show("Configure um diretório de backup antes de continuar.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var form = new BackupManagerForm(_dataService, _config))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // Recarrega as configurações após restauração
                    lblStatus.Text = "Recarregando configurações...";
                    _config = await _dataService.GetConfigAsync();
                    BindData();
                    lblStatus.Text = "Configurações atualizadas";
                }
            }
        }

// [Resto do código mantido...]