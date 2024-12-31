using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ListaCompras.Core.Models;
using ListaCompras.Core.Validators;
using ListaCompras.Core.Data;
using Microsoft.Extensions.Logging;

namespace ListaCompras.Core.Services
{
    /// <summary>
    /// Implementação do serviço de categorias
    /// </summary>
    public class CategoriaService : BaseService<CategoriaModel>, ICategoriaService
    {
        private readonly ICategoriaRepository _categoriaRepository;
        private readonly IItemRepository _itemRepository;

        public CategoriaService(
            ICategoriaRepository categoriaRepository,
            IItemRepository itemRepository,
            IValidator<CategoriaModel> validator,
            ILogger<CategoriaService> logger)
            : base(validator, logger)
        {
            _categoriaRepository = categoriaRepository;
            _itemRepository = itemRepository;
        }

        public async Task<CategoriaModel> GetByIdAsync(int id)
        {
            return await ExecuteOperationAsync(
                async () => await _categoriaRepository.GetByIdAsync(id),
                $"Obter categoria {id}");
        }

        public async Task<IEnumerable<CategoriaModel>> GetCategoriasRaizAsync()
        {
            return await ExecuteOperationAsync(
                async () => await _categoriaRepository.GetCategoriasRaizAsync(),
                "Obter categorias raiz");
        }

        public async Task<IEnumerable<CategoriaModel>> GetArvoreCategoriaAsync()
        {
            return await ExecuteOperationAsync(
                async () => await _categoriaRepository.GetArvoreCategoriaAsync(),
                "Obter árvore de categorias");
        }

        public async Task<CategoriaModel> CreateAsync(CategoriaModel categoria)
        {
            await ValidateAndThrowAsync(categoria);

            // Define ordem padrão se não especificada
            if (categoria.Ordem == 0)
            {
                var categoriasIrmas = categoria.CategoriaPaiId.HasValue
                    ? await _categoriaRepository.GetSubcategoriasAsync(categoria.CategoriaPaiId.Value)
                    : await _categoriaRepository.GetCategoriasRaizAsync();

                categoria.Ordem = categoriasIrmas.Any()
                    ? categoriasIrmas.Max(c => c.Ordem) + 1
                    : 1;
            }

            categoria.DataCriacao = DateTime.Now;
            categoria.DataAtualizacao = DateTime.Now;

            return await ExecuteOperationAsync(
                async () => await _categoriaRepository.AddAsync(categoria),
                "Criar nova categoria");
        }

        public async Task UpdateAsync(CategoriaModel categoria)
        {
            await ValidateAndThrowAsync(categoria);

            var existingCategoria = await _categoriaRepository.GetByIdAsync(categoria.Id);
            if (existingCategoria == null)
                throw new NotFoundException($"Categoria {categoria.Id} não encontrada");

            // Evita ciclos na hierarquia
            if (categoria.CategoriaPaiId.HasValue)
            {
                var subcategorias = await GetTodasSubcategoriasRecursivasAsync(categoria.Id);
                if (subcategorias.Contains(categoria.CategoriaPaiId.Value))
                    throw new InvalidOperationException("Uma categoria não pode ser subcategoria de si mesma");
            }

            categoria.DataCriacao = existingCategoria.DataCriacao;
            categoria.DataAtualizacao = DateTime.Now;

            await ExecuteOperationAsync(
                async () => await _categoriaRepository.UpdateAsync(categoria),
                $"Atualizar categoria {categoria.Id}");
        }

        public async Task DeleteAsync(int id)
        {
            var categoria = await _categoriaRepository.GetByIdAsync(id);
            if (categoria == null)
                throw new NotFoundException($"Categoria {id} não encontrada");

            // Verifica se tem subcategorias
            var subcategorias = await _categoriaRepository.GetSubcategoriasAsync(id);
            if (subcategorias.Any())
                throw new InvalidOperationException("Não é possível excluir uma categoria que possui subcategorias");

            // Verifica se tem itens
            var itens = await _itemRepository.GetByCategoriaIdAsync(id);
            if (itens.Any())
                throw new InvalidOperationException("Não é possível excluir uma categoria que possui itens");

            await ExecuteOperationAsync(
                async () => await _categoriaRepository.DeleteAsync(categoria),
                $"Excluir categoria {id}");
        }

        public async Task MoverParaCategoriaAsync(int id, int? novoCategoriaPaiId)
        {
            if (novoCategoriaPaiId.HasValue)
            {
                var categoriaPai = await _categoriaRepository.GetByIdAsync(novoCategoriaPaiId.Value);
                if (categoriaPai == null)
                    throw new NotFoundException($"Categoria pai {novoCategoriaPaiId} não encontrada");

                // Evita ciclos na hierarquia
                var subcategorias = await GetTodasSubcategoriasRecursivasAsync(id);
                if (subcategorias.Contains(novoCategoriaPaiId.Value))
                    throw new InvalidOperationException("Uma categoria não pode ser movida para uma de suas subcategorias");
            }

            await ExecuteOperationAsync(
                async () => await _categoriaRepository.MoverParaCategoriaAsync(id, novoCategoriaPaiId),
                $"Mover categoria {id} para novo pai {novoCategoriaPaiId}");
        }

        public async Task ReordenarAsync(IEnumerable<(int categoriaId, int novaOrdem)> novasOrdens)
        {
            // Valida se todas as categorias existem
            foreach (var (categoriaId, _) in novasOrdens)
            {
                var categoria = await _categoriaRepository.GetByIdAsync(categoriaId);
                if (categoria == null)
                    throw new NotFoundException($"Categoria {categoriaId} não encontrada");
            }

            await ExecuteOperationAsync(
                async () => await _categoriaRepository.ReordenarAsync(novasOrdens),
                "Reordenar categorias");
        }

        public async Task<IEnumerable<CategoriaModel>> GetSubcategoriasAsync(int categoriaId)
        {
            var categoria = await _categoriaRepository.GetByIdAsync(categoriaId);
            if (categoria == null)
                throw new NotFoundException($"Categoria {categoriaId} não encontrada");

            return await ExecuteOperationAsync(
                async () => await _categoriaRepository.GetSubcategoriasAsync(categoriaId),
                $"Obter subcategorias da categoria {categoriaId}");
        }

        public async Task AtualizarVisualizacaoAsync(int id, string cor, string icone)
        {
            var categoria = await _categoriaRepository.GetByIdAsync(id);
            if (categoria == null)
                throw new NotFoundException($"Categoria {id} não encontrada");

            // Valida cor em formato hexadecimal
            if (!string.IsNullOrEmpty(cor) && !System.Text.RegularExpressions.Regex.IsMatch(cor, "^#[0-9A-Fa-f]{6}$"))
                throw new ValidationException("Cor deve estar no formato hexadecimal (#RRGGBB)");

            categoria.Cor = cor;
            categoria.Icone = icone;
            categoria.DataAtualizacao = DateTime.Now;

            await ExecuteOperationAsync(
                async () => await _categoriaRepository.UpdateAsync(categoria),
                $"Atualizar visualização da categoria {id}");
        }

        private async Task<HashSet<int>> GetTodasSubcategoriasRecursivasAsync(int categoriaId)
        {
            var subcategorias = new HashSet<int>();
            var filhas = await _categoriaRepository.GetSubcategoriasAsync(categoriaId);

            foreach (var filha in filhas)
            {
                subcategorias.Add(filha.Id);
                var subFilhas = await GetTodasSubcategoriasRecursivasAsync(filha.Id);
                subcategorias.UnionWith(subFilhas);
            }

            return subcategorias;
        }
    }
}