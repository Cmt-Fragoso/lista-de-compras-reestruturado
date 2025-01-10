using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Interface para repositório de usuários
    /// </summary>
    public interface IUsuarioRepository : IRepository<UsuarioModel>
    {
        /// <summary>
        /// Obtém usuário por email
        /// </summary>
        Task<UsuarioModel> GetByEmailAsync(string email);

        /// <summary>
        /// Obtém usuário por ID do dispositivo
        /// </summary>
        Task<UsuarioModel> GetByDispositivoIdAsync(string dispositivoId);

        /// <summary>
        /// Verifica se email já está em uso
        /// </summary>
        Task<bool> EmailExisteAsync(string email);

        /// <summary>
        /// Atualiza senha do usuário
        /// </summary>
        Task AtualizarSenhaAsync(int usuarioId, string novaSenhaHash);

        /// <summary>
        /// Atualiza preferências do usuário
        /// </summary>
        Task AtualizarPreferenciasAsync(int usuarioId, string novasPreferencias);

        /// <summary>
        /// Atualiza status do usuário
        /// </summary>
        Task AtualizarStatusAsync(int usuarioId, StatusUsuario novoStatus);

        /// <summary>
        /// Registra acesso do usuário
        /// </summary>
        Task RegistrarAcessoAsync(int usuarioId);
    }
}