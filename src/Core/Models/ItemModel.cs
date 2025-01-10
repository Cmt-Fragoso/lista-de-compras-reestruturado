using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Linq;

namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Modelo que representa um item de compra
    /// </summary>
    public class ItemModel : BaseModel, ICloneableModel<ItemModel>, ITrackableModel<ItemModel>
    {
        #region Propriedades

        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        [StringLength(500)]
        public string Descricao { get; set; }

        [Required]
        [Range(0.001, double.MaxValue)]
        [Column(TypeName = "decimal(10, 3)")]
        public decimal Quantidade { get; set; }

        [Required]
        [StringLength(10)]
        public string Unidade { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal PrecoEstimado { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal? PrecoCompra { get; set; }

        [ForeignKey("Categoria")]
        public int? CategoriaId { get; set; }

        [JsonIgnore]
        public CategoriaModel Categoria { get; set; }

        [Required]
        [ForeignKey("Lista")]
        public int ListaId { get; set; }

        [JsonIgnore]
        public ListaModel Lista { get; set; }

        public bool IsComprado { get; set; }

        public DateTime? DataCompra { get; set; }

        [StringLength(500)]
        public string Observacao { get; set; }

        [JsonIgnore]
        public List<PrecoModel> Precos { get; set; } = new();

        [JsonIgnore]
        public List<ItemModel> HistoricoVersoes { get; set; } = new();

        #endregion

        #region Calculados

        [NotMapped]
        public decimal Total => Quantidade * (PrecoCompra ?? PrecoEstimado);

        [NotMapped]
        public decimal? Economia => PrecoEstimado > PrecoCompra ? PrecoEstimado - PrecoCompra : null;

        [NotMapped]
        public decimal? PorcentagemEconomia => Economia.HasValue ? (Economia.Value / PrecoEstimado) * 100 : null;

        #endregion

        #region Constantes

        public static readonly string[] UnidadesPadrao = new[]
        {
            "Un", "Kg", "g", "L", "ml", "m", "cm", "Pct", "Cx", "Dz"
        };

        #endregion

        #region Validação Customizada

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var error in base.Validate(validationContext))
                yield return error;

            if (PrecoCompra.HasValue && PrecoCompra.Value < 0)
                yield return new ValidationResult("Preço de compra não pode ser negativo");

            if (DataCompra.HasValue && DataCompra.Value > DateTime.Now)
                yield return new ValidationResult("Data de compra não pode ser futura");

            if (IsComprado && !PrecoCompra.HasValue)
                yield return new ValidationResult("Preço de compra é obrigatório para itens comprados");

            if (IsComprado && !DataCompra.HasValue)
                yield return new ValidationResult("Data de compra é obrigatória para itens comprados");

            if (!UnidadesPadrao.Contains(Unidade))
                yield return new ValidationResult("Unidade inválida");
        }

        #endregion

        #region Clonagem

        public ItemModel Clone()
        {
            return (ItemModel)MemberwiseClone();
        }

        public ItemModel DeepClone()
        {
            var clone = (ItemModel)MemberwiseClone();

            // Clonar coleções
            clone.Tags = new List<string>(Tags);
            clone.Metadados = new Dictionary<string, string>(Metadados);
            clone.Precos = new List<PrecoModel>();
            clone.HistoricoVersoes = new List<ItemModel>();

            return clone;
        }

        #endregion

        #region Histórico

        public void AdicionarVersao(ItemModel versao)
        {
            if (versao == null)
                throw new ArgumentNullException(nameof(versao));

            versao.Version = this.Version + 1;
            HistoricoVersoes.Add(versao);
        }

        public ItemModel ObterVersao(long version)
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

        public override TimeSpan? CacheDuration => TimeSpan.FromMinutes(60);

        public override string CacheKey => $"Item_{Id}_{Version}";

        #endregion

        #region Permissões

        public override bool PodeSerModificadoPor(int usuarioId, IEnumerable<string> permissoes)
        {
            return base.PodeSerModificadoPor(usuarioId, permissoes) &&
                   !IsComprado;
        }

        #endregion

        #region Métodos de Negócio

        public void MarcarComoComprado(decimal precoCompra, DateTime? dataCompra = null)
        {
            if (IsComprado)
                throw new InvalidOperationException("Item já está marcado como comprado");

            if (precoCompra < 0)
                throw new ArgumentException("Preço de compra não pode ser negativo");

            PrecoCompra = precoCompra;
            DataCompra = dataCompra ?? DateTime.Now;
            IsComprado = true;

            // Adiciona ao histórico de preços
            Precos.Add(new PrecoModel
            {
                ItemId = Id,
                Valor = precoCompra,
                Data = DataCompra.Value,
                IsPromocional = precoCompra < PrecoEstimado
            });
        }

        public void DesmarcarComoComprado()
        {
            if (!IsComprado)
                throw new InvalidOperationException("Item não está marcado como comprado");

            PrecoCompra = null;
            DataCompra = null;
            IsComprado = false;
        }

        public void AtualizarPrecoEstimado(decimal novoPreco)
        {
            if (novoPreco < 0)
                throw new ArgumentException("Preço estimado não pode ser negativo");

            // Guarda histórico do preço anterior
            AdicionarVersao(this.DeepClone());

            PrecoEstimado = novoPreco;
        }

        #endregion

        #region Object Overrides

        public override string ToString()
        {
            return $"{Nome} ({Quantidade} {Unidade})";
        }

        #endregion
    }
}