using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Interface para repositório de preços
    /// </summary>
    public interface IPrecoRepository : IRepository<PrecoModel>
    {
        /// <summary>
        /// Obtém histórico de preços de um item
        /// </summary>
        Task<IEnumerable<PrecoModel>> GetHistoricoAsync(int itemId);

        /// <summary>
        /// Obtém último preço registrado de um item
        /// </summary>
        Task<PrecoModel> GetUltimoPrecoAsync(int itemId);

        /// <summary>
        /// Obtém média de preço de um item em um período
        /// </summary>
        Task<decimal> GetMediaPrecoPeriodoAsync(int itemId, DateTime inicio, DateTime fim);

        /// <summary>
        /// Obtém preços por fonte
        /// </summary>
        Task<IEnumerable<PrecoModel>> GetByFonteAsync(FontePreco fonte);

        /// <summary>
        /// Obtém preços promocionais ativos
        /// </summary>
        Task<IEnumerable<PrecoModel>> GetPromocionaisAtivosAsync();

        /// <summary>
        /// Registra um novo preço para um item
        /// </summary>
        Task<PrecoModel> RegistrarPrecoAsync(int itemId, decimal valor, string local, FontePreco fonte, bool promocional = false);

        /// <summary>
        /// Salva ou atualiza um preço
        /// </summary>
        Task SaveAsync(PrecoModel preco);

        /// <summary>
        /// Obtém preços por item
        /// </summary>
        Task<IEnumerable<PrecoModel>> GetByItemIdAsync(int itemId);
    }
}
