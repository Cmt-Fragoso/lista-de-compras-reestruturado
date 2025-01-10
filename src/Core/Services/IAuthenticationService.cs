using System.Threading.Tasks;

namespace ListaCompras.Core.Services
{
    /// <summary>
    /// Interface para o serviço de autenticação
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Autentica um usuário com email e senha
        /// </summary>
        Task<AuthenticationResult> AuthenticateAsync(string email, string password);

        /// <summary>
        /// Atualiza um token de acesso usando refresh token
        /// </summary>
        Task<AuthenticationResult> RefreshTokenAsync(string accessToken, string refreshToken);

        /// <summary>
        /// Valida código 2FA para um usuário
        /// </summary>
        Task<bool> ValidateTwoFactorAsync(string email, string code);

        /// <summary>
        /// Revoga um refresh token específico
        /// </summary>
        Task RevokeTokenAsync(string refreshToken);

        /// <summary>
        /// Revoga todos os refresh tokens de um usuário
        /// </summary>
        Task RevokeAllTokensAsync(int userId);
    }
}