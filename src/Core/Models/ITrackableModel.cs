using System;
using System.Collections.Generic;

namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Interface para modelos que suportam versionamento
    /// </summary>
    public interface ITrackableModel<T> where T : class
    {
        /// <summary>
        /// Versão atual do modelo
        /// </summary>
        long Version { get; set; }

        /// <summary>
        /// Histórico de versões
        /// </summary>
        List<T> HistoricoVersoes { get; }

        /// <summary>
        /// Adiciona uma nova versão ao histórico
        /// </summary>
        void AdicionarVersao(T versao);

        /// <summary>
        /// Obtém uma versão específica do histórico
        /// </summary>
        T ObterVersao(long version);

        /// <summary>
        /// Limpa o histórico de versões
        /// </summary>
        void LimparHistorico();
    }
}