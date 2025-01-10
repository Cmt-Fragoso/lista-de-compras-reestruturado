using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ListaCompras.Core.Models;

namespace ListaCompras.UI.Services
{
    public interface IDataService
    {
        // Listas
        Task<List<ListaModel>> GetListasAsync();
        Task<ListaModel> GetListaByIdAsync(int id);
        Task<ListaModel> SaveListaAsync(ListaModel lista);
        Task DeleteListaAsync(int id);

        // Itens
        Task<List<ItemModel>> GetItensAsync();
        Task<ItemModel> GetItemByIdAsync(int id);
        Task<ItemModel> SaveItemAsync(ItemModel item);
        Task DeleteItemAsync(int id);

        // Categorias
        Task<List<CategoriaModel>> GetCategoriasAsync();
        Task<CategoriaModel> GetCategoriaByIdAsync(int id);
        Task<CategoriaModel> SaveCategoriaAsync(CategoriaModel categoria);
        Task DeleteCategoriaAsync(int id);

        // Preços
        Task<List<PrecoModel>> GetPrecosAsync(int itemId);
        Task<PrecoModel> SavePrecoAsync(PrecoModel preco);

        // Cache e Sync
        Task<DateTime> GetLastSyncAsync();
        Task SyncDataAsync();
    }
}