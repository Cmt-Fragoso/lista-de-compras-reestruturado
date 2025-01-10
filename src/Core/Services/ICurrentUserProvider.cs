namespace ListaCompras.Core.Services
{
    /// <summary>
    /// Provê informações sobre o usuário atual
    /// </summary>
    public interface ICurrentUserProvider
    {
        /// <summary>
        /// Obtém o ID do usuário atual
        /// </summary>
        int GetCurrentUserId();
        
        /// <summary>
        /// Verifica se o usuário está autenticado
        /// </summary>
        bool IsAuthenticated { get; }
        
        /// <summary>
        /// Obtém as permissões do usuário atual
        /// </summary>
        IEnumerable<string> GetPermissions();
    }
}