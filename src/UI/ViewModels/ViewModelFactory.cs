using System.Collections.Generic;
using System.Linq;
using ListaCompras.Core.Models;

namespace ListaCompras.UI.ViewModels
{
    public static class ViewModelFactory
    {
        public static ListaViewModel CreateListaViewModel(ListaModel model)
        {
            if (model == null) return null;

            return new ListaViewModel
            {
                Nome = model.Nome,
                DataAtualizacao = model.DataAtualizacao,
                IsConcluida = model.Status == StatusLista.Concluida,
                Itens = model.Itens?.ToList()
            };
        }

        public static ListaModel CreateListaModel(ListaViewModel viewModel)
        {
            if (viewModel == null) return null;

            return new ListaModel
            {
                Nome = viewModel.Nome,
                DataAtualizacao = viewModel.DataAtualizacao,
                Status = viewModel.IsConcluida ? StatusLista.Concluida : StatusLista.EmAndamento,
                Itens = viewModel.Itens?.ToList()
            };
        }

        public static ItemViewModel CreateItemViewModel(ItemModel model)
        {
            if (model == null) return null;

            return new ItemViewModel
            {
                Nome = model.Nome,
                Quantidade = model.Quantidade,
                Unidade = model.Unidade,
                Preco = model.Preco,
                Observacao = model.Observacao,
                CategoriaId = model.CategoriaId,
                CategoriaNome = model.Categoria?.Nome
            };
        }

        public static ItemModel CreateItemModel(ItemViewModel viewModel)
        {
            if (viewModel == null) return null;

            return new ItemModel
            {
                Nome = viewModel.Nome,
                Quantidade = viewModel.Quantidade,
                Unidade = viewModel.Unidade,
                Preco = viewModel.Preco,
                Observacao = viewModel.Observacao,
                CategoriaId = viewModel.CategoriaId
            };
        }

        public static List<ListaViewModel> CreateListaViewModels(IEnumerable<ListaModel> models)
        {
            return models?.Select(m => CreateListaViewModel(m)).ToList() ?? new List<ListaViewModel>();
        }

        public static List<ItemViewModel> CreateItemViewModels(IEnumerable<ItemModel> models)
        {
            return models?.Select(m => CreateItemViewModel(m)).ToList() ?? new List<ItemViewModel>();
        }
    }
}