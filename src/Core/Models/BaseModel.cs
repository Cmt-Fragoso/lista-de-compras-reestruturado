using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ListaCompras.Core.Models.Interfaces;

namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Classe base para todos os modelos com implementação das interfaces comuns
    /// </summary>
    public abstract class BaseModel : IBaseModel
    {
        #region Propriedades Básicas

        public int Id { get; set; }

        [Required]
        public DateTime DataCriacao { get; set; }

        [Required]
        public DateTime DataAtualizacao { get; set; }

        public DateTime? DataDelecao { get; set; }

        [Required]
        public int UsuarioCriacao { get; set; }

        [Required]
        public int UsuarioAtualizacao { get; set; }

        public int? UsuarioDelecao { get; set; }

        public bool Deletado { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public long Version { get; set; }

        public List<string> Tags { get; set; } = new();

        public Dictionary<string, string> Metadados { get; set; } = new();

        public string SyncId { get; set; } = Guid.NewGuid().ToString();

        public DateTime? UltimaSincronizacao { get; set; }

        #endregion

        #region Validação

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (DataCriacao == default)
                results.Add(new ValidationResult("Data de criação é obrigatória"));

            if (DataAtualizacao == default)
                results.Add(new ValidationResult("Data de atualização é obrigatória"));

            if (DataAtualizacao < DataCriacao)
                results.Add(new ValidationResult("Data de atualização não pode ser anterior à data de criação"));

            if (DataDelecao.HasValue && DataDelecao.Value < DataCriacao)
                results.Add(new ValidationResult("Data de deleção não pode ser anterior à data de criação"));

            if (UsuarioCriacao <= 0)
                results.Add(new ValidationResult("Usuário de criação inválido"));

            if (UsuarioAtualizacao <= 0)
                results.Add(new ValidationResult("Usuário de atualização inválido"));

            if (UsuarioDelecao.HasValue && UsuarioDelecao.Value <= 0)
                results.Add(new ValidationResult("Usuário de deleção inválido"));

            if (Version < 0)
                results.Add(new ValidationResult("Versão inválida"));

            if (string.IsNullOrEmpty(SyncId))
                results.Add(new ValidationResult("ID de sincronização é obrigatório"));

            ValidateMetadata(results);

            return results;
        }

        protected virtual void ValidateMetadata(List<ValidationResult> results)
        {
            if (Metadados.Any(m => string.IsNullOrWhiteSpace(m.Key)))
                results.Add(new ValidationResult("Chaves de metadados não podem ser vazias"));
        }

        public bool IsValid()
        {
            var results = new List<ValidationResult>();
            return Validator.TryValidateObject(this, new ValidationContext(this), results, true);
        }

        #endregion

        #region Versionamento

        public bool IsNewerThan(IVersionableModel other)
        {
            if (other == null) return true;
            return Version > other.Version;
        }

        #endregion

        #region Metadados

        public void SetMetadado(string chave, string valor)
        {
            if (!string.IsNullOrWhiteSpace(chave))
                Metadados[chave] = valor;
        }

        public string GetMetadado(string chave)
        {
            return Metadados.TryGetValue(chave, out var valor) ? valor : null;
        }

        public bool RemoveMetadado(string chave)
        {
            return Metadados.Remove(chave);
        }

        #endregion

        #region Tags

        public void AddTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag) && !Tags.Contains(tag))
                Tags.Add(tag);
        }

        public bool RemoveTag(string tag)
        {
            return Tags.Remove(tag);
        }

        public bool HasTag(string tag)
        {
            return Tags.Contains(tag);
        }

        #endregion

        #region Permissões

        public virtual bool PodeSerModificadoPor(int usuarioId, IEnumerable<string> permissoes)
        {
            return !Deletado && 
                   (UsuarioCriacao == usuarioId || 
                    permissoes?.Contains("Admin") == true);
        }

        public virtual bool PodeSerVisualizadoPor(int usuarioId, IEnumerable<string> permissoes)
        {
            return !Deletado || permissoes?.Contains("Admin") == true;
        }

        public virtual bool PodeSerExcluidoPor(int usuarioId, IEnumerable<string> permissoes)
        {
            return !Deletado && 
                   (UsuarioCriacao == usuarioId || 
                    permissoes?.Contains("Admin") == true);
        }

        #endregion

        #region Cache

        public virtual string CacheKey => $"{GetType().Name}_{Id}";

        public virtual TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);

        public virtual bool EnableCache => true;

        #endregion

        #region Sincronização

        public virtual bool PrecisaSincronizar =>
            !UltimaSincronizacao.HasValue ||
            DataAtualizacao > UltimaSincronizacao.Value;

        public virtual void MarcarComoSincronizado()
        {
            UltimaSincronizacao = DateTime.UtcNow;
        }

        #endregion

        #region Métodos de Objeto

        public override bool Equals(object obj)
        {
            if (obj is BaseModel other)
            {
                return Id == other.Id &&
                       Version == other.Version &&
                       !Deletado &&
                       !other.Deletado;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Version);
        }

        public override string ToString()
        {
            return $"{GetType().Name} [ID={Id}, Version={Version}]";
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Atualiza dados de auditoria
        /// </summary>
        public virtual void AtualizarAuditoria(int usuarioId)
        {
            if (Id == 0)
            {
                DataCriacao = DateTime.UtcNow;
                UsuarioCriacao = usuarioId;
                Version = 1;
            }
            else
            {
                Version++;
            }

            DataAtualizacao = DateTime.UtcNow;
            UsuarioAtualizacao = usuarioId;
        }

        /// <summary>
        /// Marca o objeto como excluído
        /// </summary>
        public virtual void MarcarComoExcluido(int usuarioId)
        {
            Deletado = true;
            DataDelecao = DateTime.UtcNow;
            UsuarioDelecao = usuarioId;
            AtualizarAuditoria(usuarioId);
        }

        #endregion
    }
}