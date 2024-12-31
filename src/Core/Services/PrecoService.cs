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
    /// Implementação do serviço de preços
    /// </summary>
    public class PrecoService : BaseService<PrecoModel>, IPrecoService
    {
        private readonly IPrecoRepository _precoRepository;
        private readonly IItemRepository _itemRepository;

        public PrecoService(
            IPrecoRepository precoRepository,
            IItemRepository itemRepository,
            IValidator<PrecoModel> validator,
            ILogger<PrecoService> logger)
            : base(validator, logger)
        {
            _precoRepository = precoRepository;
            _itemRepository = itemRepository;
        }

        public async Task<PrecoModel> GetByIdAsync(int id)
        {
            return await ExecuteOperationAsync(
                async () => await _precoRepository.GetByIdAsync(id),
                $"Obter preço {id}");
        }

        public async Task<IEnumerable<PrecoModel>> GetHistoricoAsync(int itemId)
        {
            var item = await _itemRepository.GetByIdAsync(itemId);
            if (item == null)
                throw new NotFoundException($"Item {itemId} não encontrado");

            return await ExecuteOperationAsync(
                async () => await _precoRepository.GetHistoricoAsync(itemId),
                $"Obter histórico de preços do item {itemId}");
        }

        public async Task<PrecoModel> GetUltimoPrecoAsync(int itemId)
        {
            var item = await _itemRepository.GetByIdAsync(itemId);
            if (item == null)
                throw new NotFoundException($"Item {itemId} não encontrado");

            return await ExecuteOperationAsync(
                async () => await _precoRepository.GetUltimoPrecoAsync(itemId),
                $"Obter último preço do item {itemId}");
        }

        public async Task<PrecoModel> RegistrarPrecoAsync(PrecoModel preco)
        {
            await ValidateAndThrowAsync(preco);

            // Verifica se o item existe
            var item = await _itemRepository.GetByIdAsync(preco.ItemId);
            if (item == null)
                throw new NotFoundException($"Item {preco.ItemId} não encontrado");

            preco.DataPreco = DateTime.Now;
            preco.DataCriacao = DateTime.Now;
            preco.DataAtualizacao = DateTime.Now;

            var novoPreco = await ExecuteOperationAsync(
                async () => await _precoRepository.RegistrarPrecoAsync(
                    preco.ItemId,
                    preco.Valor,
                    preco.Local,
                    preco.Fonte,
                    preco.Promocional),
                $"Registrar preço para item {preco.ItemId}");

            // Atualiza o preço estimado do item se não for promocional
            if (!preco.Promocional)
            {
                item.PrecoEstimado = preco.Valor;
                item.DataAtualizacao = DateTime.Now;
                await _itemRepository.UpdateAsync(item);
            }

            return novoPreco;
        }

        public async Task UpdateAsync(PrecoModel preco)
        {
            await ValidateAndThrowAsync(preco);

            var existingPreco = await _precoRepository.GetByIdAsync(preco.Id);
            if (existingPreco == null)
                throw new NotFoundException($"Preço {preco.Id} não encontrado");

            preco.DataCriacao = existingPreco.DataCriacao;
            preco.DataAtualizacao = DateTime.Now;

            await ExecuteOperationAsync(
                async () => await _precoRepository.UpdateAsync(preco),
                $"Atualizar preço {preco.Id}");
        }

        public async Task DeleteAsync(int id)
        {
            var preco = await _precoRepository.GetByIdAsync(id);
            if (preco == null)
                throw new NotFoundException($"Preço {id} não encontrado");

            await ExecuteOperationAsync(
                async () => await _precoRepository.DeleteAsync(preco),
                $"Excluir preço {id}");
        }

        public async Task<decimal> CalcularMediaPeriodoAsync(int itemId, DateTime inicio, DateTime fim)
        {
            if (inicio > fim)
                throw new ArgumentException("Data inicial não pode ser posterior à data final");

            var item = await _itemRepository.GetByIdAsync(itemId);
            if (item == null)
                throw new NotFoundException($"Item {itemId} não encontrado");

            return await ExecuteOperationAsync(
                async () => await _precoRepository.GetMediaPrecoPeriodoAsync(itemId, inicio, fim),
                $"Calcular média de preço do item {itemId} no período");
        }

        public async Task<IEnumerable<PrecoModel>> GetPromocionaisAtivosAsync()
        {
            return await ExecuteOperationAsync(
                async () => await _precoRepository.GetPromocionaisAtivosAsync(),
                "Obter preços promocionais ativos");
        }

        public async Task<(decimal variacao, bool tendenciaAlta)> AnalisarTendenciaAsync(int itemId, int diasAnalise)
        {
            if (diasAnalise <= 0)
                throw new ArgumentException("Período de análise deve ser maior que zero");

            var item = await _itemRepository.GetByIdAsync(itemId);
            if (item == null)
                throw new NotFoundException($"Item {itemId} não encontrado");

            var dataInicio = DateTime.Now.AddDays(-diasAnalise);
            var historico = await _precoRepository.GetHistoricoAsync(itemId);
            var precosNoPeriodo = historico.Where(p => p.DataPreco >= dataInicio)
                                         .OrderBy(p => p.DataPreco)
                                         .ToList();

            if (!precosNoPeriodo.Any())
                return (0, false);

            if (precosNoPeriodo.Count == 1)
                return (0, false);

            var precoMaisAntigo = precosNoPeriodo.First().Valor;
            var precoMaisRecente = precosNoPeriodo.Last().Valor;
            var variacao = ((precoMaisRecente - precoMaisAntigo) / precoMaisAntigo) * 100;

            return (variacao, variacao > 0);
        }

        public async Task<IEnumerable<PrecoModel>> RegistrarMultiplosAsync(IEnumerable<PrecoModel> precos)
        {
            var resultados = new List<PrecoModel>();

            foreach (var preco in precos)
            {
                try
                {
                    var resultado = await RegistrarPrecoAsync(preco);
                    resultados.Add(resultado);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Erro ao registrar preço para item {preco.ItemId}");
                    // Continua com os próximos preços mesmo se houver erro
                }
            }

            return resultados;
        }
    }
}