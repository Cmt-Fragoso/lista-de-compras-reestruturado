using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Modelo que representa um usuário do sistema
    /// </summary>
    public class UserModel : BaseModel
    {
        #region Propriedades Básicas

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [JsonIgnore]
        public byte[] PasswordHash { get; set; }

        [Required]
        [JsonIgnore]
        public byte[] PasswordSalt { get; set; }

        [Required]
        public string DeviceId { get; set; }

        [Required]
        public UserStatus Status { get; set; }

        [Required]
        public string SecurityStamp { get; set; }

        #endregion

        #region Autenticação

        public List<string> Roles { get; set; } = new();

        public bool TwoFactorEnabled { get; set; }

        [JsonIgnore]
        public string TwoFactorKey { get; set; }

        public DateTime? LastLoginDate { get; set; }

        public int FailedLoginAttempts { get; set; }

        public bool IsLockedOut { get; set; }

        public DateTime? LockoutEnd { get; set; }

        #endregion

        #region Preferências

        public string PreferredLanguage { get; set; }

        public string Theme { get; set; }

        public string TimeZone { get; set; }

        public Dictionary<string, string> Preferences { get; set; } = new();

        #endregion

        #region Relacionamentos

        [JsonIgnore]
        public List<ListaModel> Listas { get; set; } = new();

        [JsonIgnore]
        public List<ListaModel> ListasCompartilhadas { get; set; } = new();

        #endregion

        #region Notificações

        public bool NotificacoesAtivas { get; set; } = true;

        public List<string> DispositivosNotificacao { get; set; } = new();

        public Dictionary<string, bool> ConfiguracoesNotificacao { get; set; } = new();

        #endregion

        #region Validações Customizadas

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var error in base.Validate(validationContext))
                yield return error;

            if (string.IsNullOrWhiteSpace(Email))
                yield return new ValidationResult("Email é obrigatório");

            if (string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Nome é obrigatório");

            if (PasswordHash == null || PasswordHash.Length == 0)
                yield return new ValidationResult("Hash da senha é obrigatório");

            if (PasswordSalt == null || PasswordSalt.Length == 0)
                yield return new ValidationResult("Salt da senha é obrigatório");

            if (string.IsNullOrWhiteSpace(DeviceId))
                yield return new ValidationResult("ID do dispositivo é obrigatório");

            if (string.IsNullOrWhiteSpace(SecurityStamp))
                yield return new ValidationResult("Security stamp é obrigatório");

            if (IsLockedOut && !LockoutEnd.HasValue)
                yield return new ValidationResult("Data de fim do bloqueio é obrigatória quando conta está bloqueada");

            if (!IsLockedOut && LockoutEnd.HasValue)
                yield return new ValidationResult("Data de fim do bloqueio não deve existir quando conta não está bloqueada");

            if (TwoFactorEnabled && string.IsNullOrWhiteSpace(TwoFactorKey))
                yield return new ValidationResult("Chave 2FA é obrigatória quando 2FA está habilitado");
        }

        #endregion

        #region Cache

        public override bool EnableCache => true;

        public override TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);

        public override string CacheKey => $"User_{Id}_{SecurityStamp}";

        #endregion

        #region Permissões

        public override bool PodeSerModificadoPor(int usuarioId, IEnumerable<string> permissoes)
        {
            return base.PodeSerModificadoPor(usuarioId, permissoes) &&
                   (Id == usuarioId || permissoes?.Contains("Admin") == true);
        }

        public override bool PodeSerExcluidoPor(int usuarioId, IEnumerable<string> permissoes)
        {
            return base.PodeSerExcluidoPor(usuarioId, permissoes) &&
                   permissoes?.Contains("Admin") == true;
        }

        #endregion

        #region Métodos de Negócio

        public void AdicionarRole(string role)
        {
            if (!string.IsNullOrWhiteSpace(role) && !Roles.Contains(role))
            {
                Roles.Add(role);
                SecurityStamp = Guid.NewGuid().ToString();
            }
        }

        public void RemoverRole(string role)
        {
            if (Roles.Remove(role))
            {
                SecurityStamp = Guid.NewGuid().ToString();
            }
        }

        public void AtualizarPreferencia(string chave, string valor)
        {
            if (!string.IsNullOrWhiteSpace(chave))
            {
                Preferences[chave] = valor;
            }
        }

        public void RemoverPreferencia(string chave)
        {
            Preferences.Remove(chave);
        }

        public void AdicionarDispositivoNotificacao(string dispositivo)
        {
            if (!string.IsNullOrWhiteSpace(dispositivo) && 
                !DispositivosNotificacao.Contains(dispositivo))
            {
                DispositivosNotificacao.Add(dispositivo);
            }
        }

        public void RemoverDispositivoNotificacao(string dispositivo)
        {
            DispositivosNotificacao.Remove(dispositivo);
        }

        public void ConfigurarNotificacao(string tipo, bool ativa)
        {
            if (!string.IsNullOrWhiteSpace(tipo))
            {
                ConfiguracoesNotificacao[tipo] = ativa;
            }
        }

        public bool DeveReceberNotificacao(string tipo)
        {
            return NotificacoesAtivas && 
                   ConfiguracoesNotificacao.TryGetValue(tipo, out var ativa) && 
                   ativa;
        }

        public void MarcarBloqueio(TimeSpan duracao)
        {
            IsLockedOut = true;
            LockoutEnd = DateTime.UtcNow.Add(duracao);
            SecurityStamp = Guid.NewGuid().ToString();
        }

        public void RemoverBloqueio()
        {
            IsLockedOut = false;
            LockoutEnd = null;
            FailedLoginAttempts = 0;
            SecurityStamp = Guid.NewGuid().ToString();
        }

        #endregion

        #region Object Overrides

        public override string ToString()
        {
            return $"{Name} ({Email})";
        }

        #endregion
    }

    public enum UserStatus
    {
        PendingConfirmation,
        Active,
        Disabled,
        Blocked
    }
}