using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ListaCompras.Core.Models;
using ListaCompras.Core.Services;
using ListaCompras.Core.Managers;

namespace ListaCompras.UI.Services
{
    public class DataService : IDataService
    {
        private readonly ListaService _listaService;
        private readonly ItemService _itemService;
        private readonly CategoriaService _categoriaService;
        private readonly PrecoService _precoService;
        private readonly ListaComprasManager _manager;
        private readonly CacheManager _cacheManager;
        private readonly SyncManager _syncManager;

        public DataService(
            ListaService listaService,
            ItemService itemService,
            CategoriaService categoriaService,
            PrecoService precoService,
            ListaComprasManager manager,
            CacheManager cacheManager,
            SyncManager syncManager)
        {
            _listaService = listaService;
            _itemService = itemService;
            _categoriaService = categoriaService;
            _precoService = precoService;
            _manager = manager;
            _cacheManager = cacheManager;
            _syncManager = syncManager;
        }

        // Listas
        public async Task<List<ListaModel>> GetListasAsync()
        {
            try
            {
                var cacheKey = "listas_todas";
                var cached = await _cacheManager.GetAsync<List<ListaModel>>(cacheKey);
                if (cached != null)
                    return cached;

                var listas = await _listaService.GetAllAsync();
                await _cacheManager.SetAsync(cacheKey, listas, TimeSpan.FromMinutes(5));
                return listas;
            }
            catch (Exception ex)
            {
                // TODO: Logging
                throw new Exception("Erro ao buscar listas", ex);
            }
        }

        public async Task<ListaModel> GetListaByIdAsync(int id)
        {
            try
            {
                var cacheKey = $"lista_{id}";
                var cached = await _cacheManager.GetAsync<ListaModel>(cacheKey);
                if (cached != null)
                    return cached;

                var lista = await _listaService.GetByIdAsync(id);
                if (lista != null)
                    await _cacheManager.SetAsync(cacheKey, lista, TimeSpan.FromMinutes(5));
                return lista;
            }
            catch (Exception ex)
            {
                // TODO: Logging
                throw new Exception($"Erro ao buscar lista {id}", ex);
            }
        }

        public async Task<ListaModel> SaveListaAsync(ListaModel lista)
        {
            try
            {
                var saved = await _listaService.SaveAsync(lista);
                // Invalida cache
                await _cacheManager.RemoveAsync($"lista_{lista.Id}");
                await _cacheManager.RemoveAsync("listas_todas");
                return saved;
            }
            catch (Exception ex)
            {
                // TODO: Logging
                throw new Exception("Erro ao salvar lista", ex);
            }
        }

        public async Task DeleteListaAsync(int id)
        {
            try
            {
                await _listaService.DeleteAsync(id);
                // Invalida cache
                await _cacheManager.RemoveAsync($"lista_{id}");
                await _cacheManager.RemoveAsync("listas_todas");
            }
            catch (Exception ex)
            {
                // TODO: Logging
                throw new Exception($"Erro ao excluir lista {id}", ex);
            }
        }

        // Itens
        public async Task<List<ItemModel>> GetItensAsync()
        {
            try
            {
                var cacheKey = "itens_todos";
                var cached = await _cacheManager.GetAsync<List<ItemModel>>(cacheKey);
                if (cached != null)
                    return cached;

                var itens = await _itemService.GetAllAsync();
                await _cacheManager.SetAsync(cacheKey, itens, TimeSpan.FromMinutes(5));
                return itens;
            }
            catch (Exception ex)
            {
                // TODO: Logging
                throw new Exception("Erro ao buscar itens", ex);
            }
        }

        public async Task<ItemModel> GetItemByIdAsync(int id)
        {
            try
            {
                var cacheKey = $"item_{id}";
                var cached = await _cacheManager.GetAsync<ItemModel>(cacheKey);
                if (cached != null)
                    return cached;

                var item = await _itemService.GetByIdAsync(id);
                if (item != null)
                    await _cacheManager.SetAsync(cacheKey, item, TimeSpan.FromMinutes(5));
                return item;
            }
            catch (Exception ex)
            {
                // TODO: Logging
                throw new Exception($"Erro ao buscar item {id}", ex);
            }
        }

        public async Task<ItemModel> SaveItemAsync(ItemModel item)
        {
            try
            {
                var saved = await _itemService.SaveAsync(item);
                // Invalida cache
                await _cacheManager.RemoveAsync($"item_{item.Id}");
                await _cacheManager.RemoveAsync("itens_todos");
                return saved;
            }
            catch (Exception ex)
            {
                // TODO: Logging
                throw new Exception("Erro ao salvar item", ex);
            }
        }

        public async Task DeleteItemAsync(int id)
        {
            try
            {
                await _itemService.DeleteAsync(id);
                // Invalida cache
                await _cacheManager.RemoveAsync($"item_{id}");
                await _cacheManager.RemoveAsync("itens_todos");
            }
            catch (Exception ex)
            {
                // TODO: Logging
                throw new Exception($"Erro ao excluir item {id}", ex);
            }
        }

        // Categorias
        public async Task<List<CategoriaModel>> GetCategoriasAsync()
        {
            try
            {
                var cacheKey = "categorias_todas";
                var cached = await _cacheManager.GetAsync<List<CategoriaModel>>(cacheKey);
                if (cached != null)
                    return cached;

                var categorias = await _categoriaService.GetAllAsync();
                await _cacheManager.SetAsync(cacheKey, categorias, TimeSpan.FromMinutes(30));
                return categorias;
            }
            catch (Exception ex)
            {
                // TODO: Logging
                throw new Exception("Erro ao buscar categorias", ex);
            }
        }

        public async Task<CategoriaModel> GetCategoriaByIdAsync(int id)
        {
            try
            {
                var cacheKey = $"categoria_{id}";
                var cached = await _cacheManager.GetAsync<CategoriaModel>(cacheKey);
                if (cached != null)
                    return cached;

                var categoria = await _categoriaService.GetByIdAsync(id);
                if (categoria != null)
                    await _cacheManager.SetAsync(cacheKey, categoria, TimeSpan.FromMinutes(30));
                return categoria;
            }
            catch (Exception ex)
            {
                // TODO: Logging
                throw new Exception($"Erro ao buscar categoria {id}", ex);
            }
        }

        public async Task<CategoriaModel> SaveCategoriaAsync(CategoriaModel categoria)
        {
            try
            {
                var saved = await _categoriaService.SaveAsync(categoria);
                // Invalida cache
                await _cacheManager.RemoveAsync($"categoria_{categoria.Id}");
                await _cacheManager.RemoveAsync("categorias_todas");
                return saved;
            }
            catch (Exception ex)
            {
                // TODO: Logging
                throw new Exception("Erro ao salvar categoria", ex);
            }
        }

        public async Task DeleteCategoriaAsync(int id)
        {
            try
            {
                await _categoriaService.DeleteAsync(id);
                // Invalida cache
                await _cacheManager.RemoveAsync($"categoria_{id}");
                await _cacheManager.RemoveAsync("categorias_todas");
            }
            catch (Exception ex)
            {
                // TODO: Logging
                throw new Exception($"Erro ao excluir categoria {id}", ex);
            }
        }

        // Preços
        public async Task<List<PrecoModel>> GetPrecosAsync(int itemId)
        {
            try
            {
                var cacheKey = $"precos_item_{itemId}";
                var cached = await _cacheManager.GetAsync<List<PrecoModel>>(cacheKey);
                if (cached != null)
                    return cached;

                var precos = await _precoService.GetByItemIdAsync(itemId);
                await _cacheManager.SetAsync(cacheKey, precos, TimeSpan.FromMinutes(5));
                return precos;
            }
            catch (Exception ex)
            {
                // TODO: Logging
                throw new Exception($"Erro ao buscar preços do item {itemId}", ex);
            }
        }

        public async Task<PrecoModel> SavePrecoAsync(PrecoModel preco)
        {
            try
            {
                var saved = await _precoService.SaveAsync(preco);
                // Invalida cache
                await _cacheManager.RemoveAsync($"precos_item_{preco.ItemId}");
                return saved;
            }
            catch (Exception ex)
            {
                // TODO: Logging
                throw new Exception("Erro ao salvar preço", ex);
            }
        }

        // Cache e Sync
        public async Task<DateTime> GetLastSyncAsync()
        {
            return await _syncManager.GetLastSyncAsync();
        }

        public async Task SyncDataAsync()
        {
            try
            {
                await _syncManager.SyncAsync();
                // Invalida todos os caches após sync
                await _cacheManager.ClearAsync();
            }
            catch (Exception ex)
            {
                // TODO: Logging
                throw new Exception("Erro ao sincronizar dados", ex);
            }
        }
    }
}