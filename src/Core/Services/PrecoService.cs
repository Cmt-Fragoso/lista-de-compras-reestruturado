using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ListaCompras.Core.Models;
using ListaCompras.Core.Data;

namespace ListaCompras.Core.Services
{
    public class PrecoService : IPrecoService
    {
        private readonly IPrecoRepository _repository;

        public PrecoService(IPrecoRepository repository)
        {
            _repository = repository;
        }

        public async Task<PrecoModel> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<List<PrecoModel>> GetByItemIdAsync(int itemId)
        {
            return await _repository.GetByItemIdAsync(itemId);
        }

        public async Task<IEnumerable<PrecoModel>> GetHistoricoAsync(int itemId)
        {
            var precos = await _repository.GetByItemIdAsync(itemId);
            return precos.OrderByDescending(p => p.Data);
        }

        public async Task<PrecoModel> GetUltimoPrecoAsync(int itemId)
        {
            var precos = await _repository.GetByItemIdAsync(itemId);
            return precos.OrderByDescending(p => p.Data).FirstOrDefault();
        }

        public async Task<PrecoModel> RegistrarPrecoAsync(PrecoModel preco)
        {
            preco.Data = DateTime.Now;
            return await _repository.SaveAsync(preco);
        }

        public async Task<IEnumerable<PrecoModel>> RegistrarMultiplosAsync(IEnumerable<PrecoModel> precos)
        {
            var resultado = new List<PrecoModel>();
            foreach (var preco in precos)
            {
                preco.Data = DateTime.Now;
                resultado.Add(await _repository.SaveAsync(preco));
            }
            return resultado;
        }

        public async Task UpdateAsync(PrecoModel preco)
        {
            var existente = await _repository.GetByIdAsync(preco.Id);
            if (existente == null)
                throw new KeyNotFoundException($"Preço {preco.Id} não encontrado");

            await _repository.SaveAsync(preco);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task<decimal> CalcularMediaPeriodoAsync(int itemId, DateTime inicio, DateTime fim)
        {
            var precos = await _repository.GetByItemIdAsync(itemId);
            var precosNoPeriodo = precos
                .Where(p => p.Data >= inicio && p.Data <= fim)
                .ToList();

            if (!precosNoPeriodo.Any())
                return 0;

            return precosNoPeriodo.Average(p => p.Valor);
        }

        public async Task<IEnumerable<PrecoModel>> GetPromocionaisAtivosAsync()
        {
            var todos = await _repository.GetAllAsync();
            var dataLimite = DateTime.Now.AddDays(-7);
            
            return todos
                .Where(p => p.IsPromocional && p.DataFimPromocao.HasValue && p.DataFimPromocao.Value >= DateTime.Now)
                .OrderBy(p => p.DataFimPromocao);
        }

        public async Task<(decimal variacao, bool tendenciaAlta)> AnalisarTendenciaAsync(int itemId, int periodoEmDias)
        {
            var precos = await GetHistoricoAsync(itemId);
            var dataLimite = DateTime.Now.AddDays(-periodoEmDias);
            var precosNoPeriodo = precos
                .Where(p => p.Data >= dataLimite)
                .OrderBy(p => p.Data)
                .ToList();

            if (precosNoPeriodo.Count() < 2)
                return (0, false);

            var primeiro = precosNoPeriodo.First().Valor;
            var ultimo = precosNoPeriodo.Last().Valor;
            var variacao = ((ultimo - primeiro) / primeiro) * 100;

            return (variacao, variacao > 0);
        }
    }
}