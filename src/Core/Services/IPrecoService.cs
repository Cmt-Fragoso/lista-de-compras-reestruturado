using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ListaCompras.Core.Models;

namespace ListaCompras.Core.Services
{
    /// <summary>
    /// Interface para o serviço de preços
    /// </summary>
    public interface IPrecoService
    {
        /// <summary>
        /// Obtém um preço por ID
        /// </summary>
        Task<PrecoModel> GetByIdAsync(int id);

        /// <summary>
        /// Obtém histórico de preços de um item
        /// </summary>
        Task<IEnumerable<PrecoModel>> GetHistoricoAsync(int itemId);

        /// <summary>
        /// Obtém último preço registrado de um item
        /// </summary>
        Task<PrecoModel> GetUltimoPrecoAsync(int itemId);

        /// <summary>
        /// Registra um novo preço
        /// </summary>
        Task<PrecoModel> RegistrarPrecoAsync(PrecoModel preco);

        /// <summary>
        /// Atualiza um preço existente
        /// </summary>
        Task UpdateAsync(PrecoModel preco);

        /// <summary>
        /// Remove um preço
        /// </summary>
        Task DeleteAsync(int id);

        /// <summary>
        /// Calcula média de preço em um período
        /// </summary>
        Task<decimal> CalcularMediaPeriodoAsync(int itemId, DateTime inicio, DateTime fim);

        /// <summary>
        /// Obtém preços promocionais ativos
        /// </summary>
        Task<IEnumerable<PrecoModel>> GetPromocionaisAtivosAsync();

        /// <summary>
        /// Obtém análise de tendência de preços
        /// </summary>
        Task<(decimal variacao, bool tendenciaAlta)> AnalisarTendenciaAsync(int itemId, int diasAnalise);

        /// <summary>
        /// Registra múltiplos preços de uma vez
        /// </summary>
        Task<IEnumerable<PrecoModel>> RegistrarMultiplosAsync(IEnumerable<PrecoModel> precos);
    }
}