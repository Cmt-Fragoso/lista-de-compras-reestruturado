using System;
using System.Threading.Tasks;

namespace ListaCompras.Core.Managers
{
    /// <summary>
    /// Interface base para managers
    /// </summary>
    public interface IManager
    {
        /// <summary>
        /// Inicializa recursos necessários do manager
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Libera recursos utilizados pelo manager
        /// </summary>
        Task ShutdownAsync();

        /// <summary>
        /// Verifica se o manager está inicializado
        /// </summary>
        bool IsInitialized { get; }
    }
}