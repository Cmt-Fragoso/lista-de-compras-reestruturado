using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ListaCompras.Core.Models.Interfaces
{
    /// <summary>
    /// Interface base para todos os modelos que precisam de auditoria
    /// </summary>
    public interface IAuditableModel
    {
        DateTime DataCriacao { get; set; }
        DateTime DataAtualizacao { get; set; }
        DateTime? DataDelecao { get; set; }
        int UsuarioCriacao { get; set; }
        int UsuarioAtualizacao { get; set; }
        int? UsuarioDelecao { get; set; }
        bool Deletado { get; set; }
    }

    /// <summary>
    /// Interface base para modelos que suportam clonagem
    /// </summary>
    public interface ICloneableModel<T> where T : class
    {
        T Clone();
        T DeepClone();
    }

    /// <summary>
    /// Interface base para modelos que suportam serialização
    /// </summary>
    public interface ISerializableModel
    {
        string Serialize();
        void Deserialize(string data);
    }

    /// <summary>
    /// Interface base para modelos que suportam validação
    /// </summary>
    public interface IValidatableModel
    {
        IEnumerable<ValidationResult> Validate(ValidationContext validationContext);
        bool IsValid();
    }

    /// <summary>
    /// Interface base para modelos que suportam versionamento
    /// </summary>
    public interface IVersionableModel
    {
        byte[] RowVersion { get; set; }
        long Version { get; set; }
        bool IsNewerThan(IVersionableModel other);
    }

    /// <summary>
    /// Interface base para modelos que suportam metadados
    /// </summary>
    public interface IMetadataModel
    {
        Dictionary<string, string> Metadados { get; set; }
        void SetMetadado(string chave, string valor);
        string GetMetadado(string chave);
        bool RemoveMetadado(string chave);
    }

    /// <summary>
    /// Interface base para modelos que suportam tags
    /// </summary>
    public interface ITaggableModel
    {
        List<string> Tags { get; set; }
        void AddTag(string tag);
        bool RemoveTag(string tag);
        bool HasTag(string tag);
    }

    /// <summary>
    /// Interface base para modelos com permissões
    /// </summary>
    public interface IPermissionableModel
    {
        bool PodeSerModificadoPor(int usuarioId, IEnumerable<string> permissoes);
        bool PodeSerVisualizadoPor(int usuarioId, IEnumerable<string> permissoes);
        bool PodeSerExcluidoPor(int usuarioId, IEnumerable<string> permissoes);
    }

    /// <summary>
    /// Interface base para modelos com histórico
    /// </summary>
    public interface ITrackableModel<T> where T : class
    {
        List<T> HistoricoVersoes { get; set; }
        void AdicionarVersao(T versao);
        T ObterVersao(long version);
        void LimparHistorico();
    }

    /// <summary>
    /// Interface base para modelos com cache
    /// </summary>
    public interface ICacheableModel
    {
        string CacheKey { get; }
        TimeSpan? CacheDuration { get; }
        bool EnableCache { get; }
    }

    /// <summary>
    /// Interface base para modelos que precisam ser sincronizados
    /// </summary>
    public interface ISyncableModel
    {
        string SyncId { get; set; }
        DateTime? UltimaSincronizacao { get; set; }
        bool PrecisaSincronizar { get; }
        void MarcarComoSincronizado();
    }

    /// <summary>
    /// Interface base para todos os modelos
    /// </summary>
    public interface IBaseModel : 
        IAuditableModel,
        IValidatableModel,
        IVersionableModel,
        IMetadataModel,
        ITaggableModel,
        IPermissionableModel,
        ISyncableModel,
        ICacheableModel
    {
        int Id { get; set; }
    }
}