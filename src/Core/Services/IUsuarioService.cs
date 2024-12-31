using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ListaCompras.Core.Models;

namespace ListaCompras.Core.Services
{
    /// <summary>
    /// Interface para o serviço de usuários
    /// </summary>
    public interface IUsuarioService
    {
        /// <summary>
        /// Obtém um usuário por ID
        /// </summary>
        Task<UsuarioModel> GetByIdAsync(int id);

        /// <summary>
        /// Obtém um usuário por email
        /// </summary>
        Task<UsuarioModel> GetByEmailAsync(string email);

        /// <summary>
        /// Cria um novo usuário
        /// </summary>
        Task<UsuarioModel> CreateAsync(UsuarioModel usuario, string senha);

        /// <summary>
        /// Atualiza um usuário existente
        /// </summary>
        Task UpdateAsync(UsuarioModel usuario);

        /// <summary>
        /// Remove um usuário
        /// </summary>
        Task DeleteAsync(int id);

        /// <summary>
        /// Altera a senha do usuário
        /// </summary>
        Task AlterarSenhaAsync(int id, string senhaAtual, string novaSenha);

        /// <summary>
        /// Atualiza as preferências do usuário
        /// </summary>
        Task AtualizarPreferenciasAsync(int id, string novasPreferencias);

        /// <summary>
        /// Verifica credenciais de login
        /// </summary>
        Task<bool> ValidarCredenciaisAsync(string email, string senha);

        /// <summary>
        /// Registra acesso do usuário
        /// </summary>
        Task RegistrarAcessoAsync(int id);

        /// <summary>
        /// Inicia processo de recuperação de senha
        /// </summary>
        Task IniciarRecuperacaoSenhaAsync(string email);

        /// <summary>
        /// Atualiza status do usuário
        /// </summary>
        Task AtualizarStatusAsync(int id, StatusUsuario novoStatus);
    }
}