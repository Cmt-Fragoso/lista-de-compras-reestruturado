using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using ListaCompras.UI.Controls;
using ListaCompras.UI.Themes;
using ListaCompras.UI.ViewModels;
using ListaCompras.UI.Services;

namespace ListaCompras.UI.Forms
{
    public class ListaEditForm : Form
    {
        private readonly IDataService _dataService;
        private readonly ListaViewModel _viewModel;
        private Panel toolbarPanel;
        private Panel contentPanel;
        private BaseButton btnSalvar;
        private BaseButton btnCancelar;
        private BaseTextBox txtNome;
        private Label lblNome;
        private Panel itensPanel;
        private ListView itensListView;
        private BaseButton btnAddItem;
        private BaseButton btnRemoverItem;
        private Label lblTotal;
        private Label lblStatus;

        public ListaEditForm(IDataService dataService, ListaViewModel viewModel = null)
        {
            _dataService = dataService;
            _viewModel = viewModel ?? new ListaViewModel();
            InitializeComponent();
            ConfigureTheme();
            BindData();
        }

        public ListaViewModel Lista => _viewModel;

        private void InitializeComponent()
        {
            // [O restante do código do InitializeComponent continua igual...]
            // Apenas adicionando o lblStatus:
            lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Controls.Add(lblStatus);
        }

        private async void BtnAddItem_Click(object sender, EventArgs e)
        {
            using (var itemForm = new ItemEditForm(_dataService))
            {
                if (itemForm.ShowDialog() == DialogResult.OK)
                {
                    var item = itemForm.Item;
                    if (_viewModel.Itens == null)
                        _viewModel.Itens = new List<Core.Models.ItemModel>();

                    try
                    {
                        lblStatus.Text = "Salvando item...";
                        var savedItem = await _dataService.SaveItemAsync(ViewModelFactory.CreateItemModel(item));
                        var savedViewModel = ViewModelFactory.CreateItemViewModel(savedItem);
                        
                        _viewModel.Itens.Add(savedItem);
                        AddItemToListView(savedViewModel);
                        AtualizarTotal();
                        lblStatus.Text = "Item adicionado com sucesso";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao salvar item: {ex.Message}", "Erro",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        lblStatus.Text = "Erro ao salvar item";
                    }
                }
            }
        }

        private async void ItensListView_DoubleClick(object sender, EventArgs e)
        {
            if (itensListView.SelectedItems.Count == 0) return;

            var selectedItem = itensListView.SelectedItems[0];
            var item = selectedItem.Tag as ItemViewModel;

            using (var itemForm = new ItemEditForm(_dataService, item))
            {
                if (itemForm.ShowDialog() == DialogResult.OK)
                {
                    var updatedItem = itemForm.Item;
                    
                    try
                    {
                        lblStatus.Text = "Atualizando item...";
                        var savedItem = await _dataService.SaveItemAsync(ViewModelFactory.CreateItemModel(updatedItem));
                        var savedViewModel = ViewModelFactory.CreateItemViewModel(savedItem);
                        
                        // Atualiza o item na lista
                        var index = _viewModel.Itens.FindIndex(i => i.Id == savedItem.Id);
                        if (index >= 0)
                        {
                            _viewModel.Itens[index] = savedItem;
                        }

                        // Atualiza o item na ListView
                        selectedItem.Text = savedViewModel.Nome;
                        selectedItem.SubItems[1].Text = savedViewModel.Quantidade.ToString("N3");
                        selectedItem.SubItems[2].Text = savedViewModel.Unidade;
                        selectedItem.SubItems[3].Text = savedViewModel.Preco.ToString("C2");
                        selectedItem.SubItems[4].Text = savedViewModel.Total.ToString("C2");
                        selectedItem.Tag = savedViewModel;

                        AtualizarTotal();
                        lblStatus.Text = "Item atualizado com sucesso";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao atualizar item: {ex.Message}", "Erro",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        lblStatus.Text = "Erro ao atualizar item";
                    }
                }
            }
        }

        private async void BtnRemoverItem_Click(object sender, EventArgs e)
        {
            if (itensListView.SelectedItems.Count == 0) return;

            var selectedItem = itensListView.SelectedItems[0];
            var item = selectedItem.Tag as ItemViewModel;

            if (MessageBox.Show("Deseja realmente remover este item?",
                "Confirmar Exclusão", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    lblStatus.Text = "Removendo item...";
                    await _dataService.DeleteItemAsync(item.Id);
                    _viewModel.Itens.RemoveAll(i => i.Id == item.Id);
                    itensListView.Items.Remove(selectedItem);
                    AtualizarTotal();
                    lblStatus.Text = "Item removido com sucesso";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao remover item: {ex.Message}", "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = "Erro ao remover item";
                }
            }
        }

        // [O resto dos métodos continua igual...]
    }
}