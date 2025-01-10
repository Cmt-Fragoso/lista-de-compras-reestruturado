using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Linq;

namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Modelo que representa uma lista de compras
    /// </summary>
    public class ListaModel : BaseModel, ICloneableModel<ListaModel>, ITrackableModel<ListaModel>
    {
        #region Propriedades

        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        [StringLength(500)]
        public string Descricao { get; set; }

        [Required]
        [ForeignKey("Usuario")]
        public int UsuarioId { get; set; }

        [JsonIgnore]
        public UsuarioModel Usuario { get; set; }

        [JsonIgnore]
        public List<ItemModel> Itens { get; set; } = new();

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal OrcamentoPrevisto { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal? OrcamentoGasto { get; set; }

        public StatusLista Status { get; set; }

        public bool Arquivada { get; set; }

        public DateTime? DataArquivamento { get; set; }

        public bool Compartilhada { get; set; }

        public List<int> UsuariosCompartilhados { get; set; } = new();

        public DateTime? DataPrevisaoCompra { get; set; }

        public string Local { get; set; }

        [JsonIgnore]
        public List<ListaModel> HistoricoVersoes { get; set; } = new();

        #endregion

        #region Calculados

        [NotMapped]
        public decimal TotalEstimado => Itens?.Sum(i => i.Quantidade * i.PrecoEstimado) ?? 0;

        [NotMapped]
        public decimal TotalComprado => Itens?.Where(i => i.IsComprado)
                                            .Sum(i => i.Quantidade * i.PrecoCompra.Value) ?? 0;

        [NotMapped]
        public decimal? Economia => Itens?.Where(i => i.IsComprado)
                                       .Sum(i => i.Economia * i.Quantidade) ?? 0;

        [NotMapped]
        public decimal PorcentagemConcluida => Itens?.Any() == true
            ? (Itens.Count(i => i.IsComprado) / (decimal)Itens.Count) * 100
            : 0;

        [NotMapped]
        public bool DentroOrcamento => OrcamentoPrevisto == 0 || 
                                     (OrcamentoGasto ?? TotalComprado) <= OrcamentoPrevisto;

        #endregion

        #region Validação Customizada

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var error in base.Validate(validationContext))
                yield return error;

            if (Arquivada && !DataArquivamento.HasValue)
                yield return new ValidationResult("Data de arquivamento é obrigatória para listas arquivadas");

            if (DataPrevisaoCompra.HasValue && DataPrevisaoCompra.Value < DateTime.Today)
                yield return new ValidationResult("Data de previsão não pode ser no passado");

            if (OrcamentoGasto.HasValue && OrcamentoGasto.Value < 0)
                yield return new ValidationResult("Orçamento gasto não pode ser negativo");

            if (Status == StatusLista.Concluida && !Itens.All(i => i.IsComprado))
                yield return new ValidationResult("Todos os itens devem estar comprados para concluir a lista");

            if (Status == StatusLista.Arquivada && !Arquivada)
                yield return new ValidationResult("Lista com status Arquivada deve estar marcada como arquivada");
        }

        #endregion

        #region Clonagem

        public ListaModel Clone()
        {
            return (ListaModel)MemberwiseClone();
        }

        public ListaModel DeepClone()
        {
            var clone = (ListaModel)MemberwiseClone();

            // Clonar coleções
            clone.Tags = new List<string>(Tags);
            clone.Metadados = new Dictionary<string, string>(Metadados);
            clone.Itens = Itens.Select(i => i.DeepClone()).ToList();
            clone.UsuariosCompartilhados = new List<int>(UsuariosCompartilhados);
            clone.HistoricoVersoes = new List<ListaModel>();

            return clone;
        }

        #endregion

        #region Histórico

        public void AdicionarVersao(ListaModel versao)
        {
            if (versao == null)
                throw new ArgumentNullException(nameof(versao));

            versao.Version = this.Version + 1;
            HistoricoVersoes.Add(versao);
        }

        public ListaModel ObterVersao(long version)
        {
            return HistoricoVersoes.FirstOrDefault(v => v.Version == version);
        }

        public void LimparHistorico()
        {
            HistoricoVersoes.Clear();
        }

        #endregion

        #region Cache

        public override bool EnableCache => true;

        public override TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);

        public override string CacheKey => $"Lista_{Id}_{Version}";

        #endregion

        #region Permissões

        public override bool PodeSerModificadoPor(int usuarioId, IEnumerable<string> permissoes)
        {
            return base.PodeSerModificadoPor(usuarioId, permissoes) &&
                   !Arquivada &&
                   (UsuarioId == usuarioId || 
                    UsuariosCompartilhados.Contains(usuarioId) || 
                    permissoes?.Contains("Admin") == true);
        }

        public override bool PodeSerVisualizadoPor(int usuarioId, IEnumerable<string> permissoes)
        {
            return base.PodeSerVisualizadoPor(usuarioId, permissoes) &&
                   (UsuarioId == usuarioId || 
                    UsuariosCompartilhados.Contains(usuarioId) || 
                    permissoes?.Contains("Admin") == true);
        }

        #endregion

        #region Métodos de Negócio

        public void AtualizarStatus()
        {
            if (Arquivada)
            {
                Status = StatusLista.Arquivada;
                return;
            }

            if (!Itens.Any())
            {
                Status = StatusLista.EmEdicao;
                return;
            }

            if (Itens.All(i => i.IsComprado))
            {
                Status = StatusLista.Concluida;
                return;
            }

            if (Itens.Any(i => i.IsComprado))
            {
                Status = StatusLista.EmCompra;
                return;
            }

            Status = StatusLista.EmEdicao;
        }

        public void Arquivar()
        {
            if (Arquivada)
                throw new InvalidOperationException("Lista já está arquivada");

            Arquivada = true;
            DataArquivamento = DateTime.UtcNow;
            Status = StatusLista.Arquivada;
        }

        public void Desarquivar()
        {
            if (!Arquivada)
                throw new InvalidOperationException("Lista não está arquivada");

            Arquivada = false;
            DataArquivamento = null;
            AtualizarStatus();
        }

        public void Compartilhar(int usuarioId)
        {
            if (usuarioId <= 0)
                throw new ArgumentException("ID de usuário inválido");

            if (usuarioId == UsuarioId)
                throw new InvalidOperationException("Não é possível compartilhar com o próprio dono");

            if (!UsuariosCompartilhados.Contains(usuarioId))
            {
                UsuariosCompartilhados.Add(usuarioId);
                Compartilhada = true;
            }
        }

        public void RemoverCompartilhamento(int usuarioId)
        {
            if (UsuariosCompartilhados.Remove(usuarioId) && !UsuariosCompartilhados.Any())
            {
                Compartilhada = false;
            }
        }

        public void DefinirOrcamento(decimal orcamento)
        {
            if (orcamento < 0)
                throw new ArgumentException("Orçamento não pode ser negativo");

            if (Status == StatusLista.Concluida || Status == StatusLista.Arquivada)
                throw new InvalidOperationException("Não é possível alterar orçamento de lista concluída ou arquivada");

            OrcamentoPrevisto = orcamento;
        }

        public void RegistrarGasto(decimal gasto)
        {
            if (gasto < 0)
                throw new ArgumentException("Gasto não pode ser negativo");

            OrcamentoGasto = (OrcamentoGasto ?? 0) + gasto;
        }

        #endregion

        #region Object Overrides

        public override string ToString()
        {
            return $"{Nome} ({Status})";
        }

        #endregion
    }

    public enum StatusLista
    {
        EmEdicao,
        EmCompra,
        Concluida,
        Arquivada
    }
}