using System.Threading.Tasks;
using ListaCompras.Core.Models;

namespace ListaCompras.Core.Services
{
    /// <summary>
    /// Interface para o serviço de gerenciamento de usuários
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Cria um novo usuário
        /// </summary>
        Task<UserModel> CreateAsync(UserRegistrationModel registration);

        /// <summary>
        /// Atualiza um usuário existente
        /// </summary>
        Task<UserModel> UpdateAsync(UserModel user);

        /// <summary>
        /// Altera a senha do usuário
        /// </summary>
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

        /// <summary>
        /// Habilita autenticação de dois fatores
        /// </summary>
        Task<bool> EnableTwoFactorAsync(int userId);

        /// <summary>
        /// Desabilita autenticação de dois fatores
        /// </summary>
        Task<bool> DisableTwoFactorAsync(int userId);

        /// <summary>
        /// Atualiza a data do último login
        /// </summary>
        Task UpdateLastLoginAsync(int userId);

        /// <summary>
        /// Obtém usuário por ID
        /// </summary>
        Task<UserModel> GetByIdAsync(int id);

        /// <summary>
        /// Obtém usuário por email
        /// </summary>
        Task<UserModel> GetByEmailAsync(string email);

        /// <summary>
        /// Confirma o email do usuário
        /// </summary>
        Task<bool> ConfirmEmailAsync(string email, string token);
    }
}