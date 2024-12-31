using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using ListaCompras.Core.Models;
using ListaCompras.Core.Validators;
using ListaCompras.Core.Data;
using Microsoft.Extensions.Logging;

namespace ListaCompras.Core.Services
{
    /// <summary>
    /// Implementação do serviço de usuários
    /// </summary>
    public class UsuarioService : BaseService<UsuarioModel>, IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;

        public UsuarioService(
            IUsuarioRepository usuarioRepository,
            IValidator<UsuarioModel> validator,
            ILogger<UsuarioService> logger)
            : base(validator, logger)
        {
            _usuarioRepository = usuarioRepository;
        }

        public async Task<UsuarioModel> GetByIdAsync(int id)
        {
            return await ExecuteOperationAsync(
                async () => await _usuarioRepository.GetByIdAsync(id),
                $"Obter usuário {id}");
        }

        public async Task<UsuarioModel> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email não pode ser vazio");

            return await ExecuteOperationAsync(
                async () => await _usuarioRepository.GetByEmailAsync(email),
                $"Obter usuário por email {email}");
        }

        public async Task<UsuarioModel> CreateAsync(UsuarioModel usuario, string senha)
        {
            if (string.IsNullOrWhiteSpace(senha))
                throw new ArgumentException("Senha não pode ser vazia");

            usuario.SenhaHash = GerarHashSenha(senha);
            await ValidateAndThrowAsync(usuario);

            // Verifica se email já existe
            var emailExiste = await _usuarioRepository.EmailExisteAsync(usuario.Email);
            if (emailExiste)
                throw new InvalidOperationException("Email já está em uso");

            usuario.DataCriacao = DateTime.Now;
            usuario.DataAtualizacao = DateTime.Now;
            usuario.Status = StatusUsuario.PendenteConfirmacao;

            return await ExecuteOperationAsync(
                async () => await _usuarioRepository.AddAsync(usuario),
                "Criar novo usuário");
        }

        public async Task UpdateAsync(UsuarioModel usuario)
        {
            await ValidateAndThrowAsync(usuario);

            var existingUsuario = await _usuarioRepository.GetByIdAsync(usuario.Id);
            if (existingUsuario == null)
                throw new NotFoundException($"Usuário {usuario.Id} não encontrado");

            // Mantém hash da senha e status existentes
            usuario.SenhaHash = existingUsuario.SenhaHash;
            usuario.Status = existingUsuario.Status;
            usuario.DataCriacao = existingUsuario.DataCriacao;
            usuario.DataAtualizacao = DateTime.Now;

            await ExecuteOperationAsync(
                async () => await _usuarioRepository.UpdateAsync(usuario),
                $"Atualizar usuário {usuario.Id}");
        }

        public async Task DeleteAsync(int id)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(id);
            if (usuario == null)
                throw new NotFoundException($"Usuário {id} não encontrado");

            await ExecuteOperationAsync(
                async () => await _usuarioRepository.DeleteAsync(usuario),
                $"Excluir usuário {id}");
        }

        public async Task AlterarSenhaAsync(int id, string senhaAtual, string novaSenha)
        {
            if (string.IsNullOrWhiteSpace(senhaAtual))
                throw new ArgumentException("Senha atual não pode ser vazia");

            if (string.IsNullOrWhiteSpace(novaSenha))
                throw new ArgumentException("Nova senha não pode ser vazia");

            var usuario = await _usuarioRepository.GetByIdAsync(id);
            if (usuario == null)
                throw new NotFoundException($"Usuário {id} não encontrado");

            // Valida senha atual
            var hashSenhaAtual = GerarHashSenha(senhaAtual);
            if (hashSenhaAtual != usuario.SenhaHash)
                throw new UnauthorizedAccessException("Senha atual incorreta");

            // Gera hash da nova senha
            var novoHashSenha = GerarHashSenha(novaSenha);

            await ExecuteOperationAsync(
                async () => await _usuarioRepository.AtualizarSenhaAsync(id, novoHashSenha),
                $"Alterar senha do usuário {id}");
        }

        public async Task AtualizarPreferenciasAsync(int id, string novasPreferencias)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(id);
            if (usuario == null)
                throw new NotFoundException($"Usuário {id} não encontrado");

            // Valida se é um JSON válido
            try
            {
                System.Text.Json.JsonDocument.Parse(novasPreferencias);
            }
            catch (System.Text.Json.JsonException)
            {
                throw new ArgumentException("Preferências devem estar em formato JSON válido");
            }

            await ExecuteOperationAsync(
                async () => await _usuarioRepository.AtualizarPreferenciasAsync(id, novasPreferencias),
                $"Atualizar preferências do usuário {id}");
        }

        public async Task<bool> ValidarCredenciaisAsync(string email, string senha)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
                return false;

            var usuario = await _usuarioRepository.GetByEmailAsync(email);
            if (usuario == null)
                return false;

            var hashSenha = GerarHashSenha(senha);
            return hashSenha == usuario.SenhaHash;
        }

        public async Task RegistrarAcessoAsync(int id)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(id);
            if (usuario == null)
                throw new NotFoundException($"Usuário {id} não encontrado");

            await ExecuteOperationAsync(
                async () => await _usuarioRepository.RegistrarAcessoAsync(id),
                $"Registrar acesso do usuário {id}");
        }

        public async Task IniciarRecuperacaoSenhaAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email não pode ser vazio");

            var usuario = await _usuarioRepository.GetByEmailAsync(email);
            if (usuario == null)
                throw new NotFoundException($"Usuário com email {email} não encontrado");

            // Aqui seria implementada a lógica de recuperação de senha
            // Por exemplo, gerar token, salvar no banco e enviar email
            throw new NotImplementedException("Sistema de recuperação de senha ainda não implementado");
        }

        public async Task AtualizarStatusAsync(int id, StatusUsuario novoStatus)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(id);
            if (usuario == null)
                throw new NotFoundException($"Usuário {id} não encontrado");

            await ExecuteOperationAsync(
                async () => await _usuarioRepository.AtualizarStatusAsync(id, novoStatus),
                $"Atualizar status do usuário {id}");
        }

        private string GerarHashSenha(string senha)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(senha);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}