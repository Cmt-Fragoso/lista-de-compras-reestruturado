using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ListaCompras.Core.Models;
using ListaCompras.Core.Data;

namespace ListaCompras.Core.Services
{
    public class UserService : IUserService
    {
        private readonly RuntimeAppDbContextFactory _contextFactory;
        private readonly ILogger<UserService> _logger;
        private readonly UserSettings _settings;

        public UserService(
            RuntimeAppDbContextFactory contextFactory,
            ILogger<UserService> logger,
            IOptions<UserSettings> settings)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<UserModel> CreateAsync(UserRegistrationModel registration)
        {
            try
            {
                // Validar entrada
                await ValidateRegistrationAsync(registration);

                // Gerar salt e hash da senha
                using var hmac = new HMACSHA512();
                var passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(registration.Password));
                var passwordSalt = hmac.Key;

                // Gerar chave 2FA
                var twoFactorKey = GenerateTwoFactorKey();

                // Criar usuário
                var user = new UserModel
                {
                    Name = registration.Name,
                    Email = registration.Email.ToLowerInvariant(),
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    DeviceId = registration.DeviceId,
                    TwoFactorEnabled = _settings.RequireTwoFactor,
                    TwoFactorKey = twoFactorKey,
                    Status = UserStatus.PendingConfirmation,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    Roles = new List<string> { "User" }
                };

                await using var context = await _contextFactory.CreateAsync();
                context.Users.Add(user);
                await context.SaveChangesAsync();

                // Enviar email de confirmação
                await SendConfirmationEmailAsync(user);

                _logger.LogInformation("Usuário criado: {Email}", user.Email);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar usuário: {Email}", registration.Email);
                throw;
            }
        }

        public async Task<UserModel> UpdateAsync(UserModel user)
        {
            try
            {
                await using var context = await _contextFactory.CreateAsync();
                context.Users.Update(user);
                await context.SaveChangesAsync();

                _logger.LogInformation("Usuário atualizado: {Email}", user.Email);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar usuário: {Email}", user.Email);
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(
            int userId, 
            string currentPassword, 
            string newPassword)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                // Verificar senha atual
                using (var hmac = new HMACSHA512(user.PasswordSalt))
                {
                    var computedHash = hmac.ComputeHash(
                        System.Text.Encoding.UTF8.GetBytes(currentPassword));
                    
                    if (!computedHash.SequenceEqual(user.PasswordHash))
                        return false;
                }

                // Validar nova senha
                ValidatePassword(newPassword);

                // Gerar novo hash
                using (var hmac = new HMACSHA512())
                {
                    user.PasswordHash = hmac.ComputeHash(
                        System.Text.Encoding.UTF8.GetBytes(newPassword));
                    user.PasswordSalt = hmac.Key;
                }

                // Atualizar security stamp
                user.SecurityStamp = Guid.NewGuid().ToString();

                await UpdateAsync(user);
                
                _logger.LogInformation("Senha alterada: {Email}", user.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao alterar senha do usuário ID {Id}", userId);
                throw;
            }
        }

        public async Task<bool> EnableTwoFactorAsync(int userId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                user.TwoFactorEnabled = true;
                user.TwoFactorKey = GenerateTwoFactorKey();
                user.SecurityStamp = Guid.NewGuid().ToString();

                await UpdateAsync(user);
                
                _logger.LogInformation("2FA habilitado: {Email}", user.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao habilitar 2FA para usuário ID {Id}", userId);
                throw;
            }
        }

        public async Task<bool> DisableTwoFactorAsync(int userId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null || _settings.RequireTwoFactor)
                    return false;

                user.TwoFactorEnabled = false;
                user.TwoFactorKey = null;
                user.SecurityStamp = Guid.NewGuid().ToString();

                await UpdateAsync(user);
                
                _logger.LogInformation("2FA desabilitado: {Email}", user.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desabilitar 2FA para usuário ID {Id}", userId);
                throw;
            }
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user != null)
                {
                    user.LastLoginDate = DateTime.UtcNow;
                    user.FailedLoginAttempts = 0;
                    user.IsLockedOut = false;
                    user.LockoutEnd = null;

                    await UpdateAsync(user);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar último login do usuário ID {Id}", userId);
                throw;
            }
        }

        public async Task<UserModel> GetByIdAsync(int id)
        {
            try
            {
                await using var context = await _contextFactory.CreateReadOnlyAsync();
                return await context.Users.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuário ID {Id}", id);
                throw;
            }
        }

        public async Task<UserModel> GetByEmailAsync(string email)
        {
            try
            {
                await using var context = await _contextFactory.CreateReadOnlyAsync();
                return await context.Users
                    .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuário por email: {Email}", email);
                throw;
            }
        }

        public async Task<bool> ConfirmEmailAsync(string email, string token)
        {
            try
            {
                var user = await GetByEmailAsync(email);
                if (user == null || user.Status != UserStatus.PendingConfirmation)
                    return false;

                // Validar token
                if (!ValidateEmailConfirmationToken(user, token))
                    return false;

                user.Status = UserStatus.Active;
                await UpdateAsync(user);

                _logger.LogInformation("Email confirmado: {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao confirmar email: {Email}", email);
                throw;
            }
        }

        private async Task ValidateRegistrationAsync(UserRegistrationModel registration)
        {
            // Validar email
            if (string.IsNullOrWhiteSpace(registration.Email))
                throw new ValidationException("Email é obrigatório");

            if (!IsValidEmail(registration.Email))
                throw new ValidationException("Email inválido");

            // Verificar email existente
            var existingUser = await GetByEmailAsync(registration.Email);
            if (existingUser != null)
                throw new ValidationException("Email já cadastrado");

            // Validar senha
            ValidatePassword(registration.Password);

            // Validar nome
            if (string.IsNullOrWhiteSpace(registration.Name))
                throw new ValidationException("Nome é obrigatório");

            if (registration.Name.Length < 3 || registration.Name.Length > 100)
                throw new ValidationException("Nome deve ter entre 3 e 100 caracteres");

            // Validar device
            if (string.IsNullOrWhiteSpace(registration.DeviceId))
                throw new ValidationException("ID do dispositivo é obrigatório");
        }

        private void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ValidationException("Senha é obrigatória");

            if (password.Length < _settings.MinPasswordLength)
                throw new ValidationException(
                    $"Senha deve ter no mínimo {_settings.MinPasswordLength} caracteres");

            if (_settings.RequireDigit && !password.Any(char.IsDigit))
                throw new ValidationException("Senha deve conter números");

            if (_settings.RequireLowercase && !password.Any(char.IsLower))
                throw new ValidationException("Senha deve conter letras minúsculas");

            if (_settings.RequireUppercase && !password.Any(char.IsUpper))
                throw new ValidationException("Senha deve conter letras maiúsculas");

            if (_settings.RequireSpecialCharacter && 
                !password.Any(c => !char.IsLetterOrDigit(c)))
                throw new ValidationException("Senha deve conter caracteres especiais");
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateTwoFactorKey()
        {
            var key = new byte[20];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(key);
            }
            return Convert.ToBase64String(key);
        }

        private bool ValidateEmailConfirmationToken(UserModel user, string token)
        {
            // TODO: Implementar validação real do token de confirmação
            return true;
        }

        private async Task SendConfirmationEmailAsync(UserModel user)
        {
            // TODO: Implementar envio real de email de confirmação
            _logger.LogInformation("Email de confirmação enviado: {Email}", user.Email);
        }
    }

    public class UserSettings
    {
        public int MinPasswordLength { get; set; } = 8;
        public bool RequireDigit { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireSpecialCharacter { get; set; } = true;
        public bool RequireTwoFactor { get; set; } = false;
        public int TokenExpirationMinutes { get; set; } = 60;
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }

    public class UserRegistrationModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string DeviceId { get; set; }
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message)
        {
        }
    }
}