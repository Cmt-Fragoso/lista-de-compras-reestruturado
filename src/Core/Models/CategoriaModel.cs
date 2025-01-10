using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Drawing;
using System.Linq;

namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Modelo que representa uma categoria de itens
    /// </summary>
    public class CategoriaModel : BaseModel, ICloneableModel<CategoriaModel>
    {
        #region Propriedades

        [Required]
        [StringLength(50)]
        public string Nome { get; set; }

        [StringLength(200)]
        public string Descricao { get; set; }

        [Required]
        [StringLength(7)]
        [RegularExpression("^#[0-9A-Fa-f]{6}$")]
        public string Cor { get; set; }

        [StringLength(50)]
        public string Icone { get; set; }

        public int Ordem { get; set; }

        [ForeignKey("CategoriaPai")]
        public int? CategoriaPaiId { get; set; }

        [JsonIgnore]
        public CategoriaModel CategoriaPai { get; set; }

        [JsonIgnore]
        public List<CategoriaModel> SubCategorias { get; set; } = new();

        [JsonIgnore]
        public List<ItemModel> Itens { get; set; } = new();

        [JsonIgnore]
        public bool IsPadrao { get; set; }

        #endregion

        #region Calculados

        [NotMapped]
        public bool TemSubcategorias => SubCategorias?.Any() == true;

        [NotMapped]
        public int NivelProfundidade => CalcularNivel();

        [NotMapped]
        public string CaminhoCompleto => GerarCaminhoCompleto();

        [NotMapped]
        public int TotalItens => Itens.Count + SubCategorias.Sum(s => s.TotalItens);

        [NotMapped]
        public decimal TotalValor => Itens.Sum(i => i.Total) + SubCategorias.Sum(s => s.TotalValor);

        #endregion

        #region Validação Customizada

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var error in base.Validate(validationContext))
                yield return error;

            if (CategoriaPaiId == Id)
                yield return new ValidationResult("Categoria não pode ser pai dela mesma");

            if (CategoriaPaiId.HasValue && TemCiclo())
                yield return new ValidationResult("Ciclo detectado na hierarquia de categorias");

            if (string.IsNullOrEmpty(Cor) || !Cor.StartsWith("#"))
                yield return new ValidationResult("Cor deve estar no formato hexadecimal (#RRGGBB)");

            try
            {
                ColorTranslator.FromHtml(Cor);
            }
            catch
            {
                yield return new ValidationResult("Cor inválida");
            }
        }

        #endregion

        #region Clonagem

        public CategoriaModel Clone()
        {
            return (CategoriaModel)MemberwiseClone();
        }

        public CategoriaModel DeepClone()
        {
            var clone = (CategoriaModel)MemberwiseClone();

            // Clonar coleções
            clone.Tags = new List<string>(Tags);
            clone.Metadados = new Dictionary<string, string>(Metadados);
            clone.SubCategorias = SubCategorias.Select(s => s.DeepClone()).ToList();
            clone.Itens = new List<ItemModel>();  // Não clona itens

            return clone;
        }

        #endregion

        #region Cache

        public override bool EnableCache => true;

        public override TimeSpan? CacheDuration => TimeSpan.FromHours(1);

        public override string CacheKey => $"Categoria_{Id}_{Version}";

        #endregion

        #region Permissões

        public override bool PodeSerModificadoPor(int usuarioId, IEnumerable<string> permissoes)
        {
            return base.PodeSerModificadoPor(usuarioId, permissoes) &&
                   !IsPadrao;
        }

        public override bool PodeSerExcluidoPor(int usuarioId, IEnumerable<string> permissoes)
        {
            return base.PodeSerExcluidoPor(usuarioId, permissoes) &&
                   !IsPadrao &&
                   !TemSubcategorias &&
                   !Itens.Any();
        }

        #endregion

        #region Métodos de Negócio

        public void AdicionarSubcategoria(CategoriaModel subcategoria)
        {
            if (subcategoria == null)
                throw new ArgumentNullException(nameof(subcategoria));

            if (subcategoria.Id == Id)
                throw new InvalidOperationException("Categoria não pode ser subcategoria dela mesma");

            if (subcategoria.TemCiclo())
                throw new InvalidOperationException("Operação criaria um ciclo na hierarquia");

            subcategoria.CategoriaPaiId = Id;
            SubCategorias.Add(subcategoria);
        }

        public void RemoverSubcategoria(CategoriaModel subcategoria)
        {
            if (subcategoria == null)
                throw new ArgumentNullException(nameof(subcategoria));

            if (SubCategorias.Remove(subcategoria))
            {
                subcategoria.CategoriaPaiId = null;
            }
        }

        public void MoverPara(CategoriaModel novoPai)
        {
            if (novoPai?.Id == Id)
                throw new InvalidOperationException("Categoria não pode ser movida para ela mesma");

            if (novoPai != null && (novoPai.Id == CategoriaPaiId || TemDescendente(novoPai.Id)))
                throw new InvalidOperationException("Operação criaria um ciclo na hierarquia");

            CategoriaPai?.SubCategorias.Remove(this);
            CategoriaPaiId = novoPai?.Id;
            novoPai?.SubCategorias.Add(this);
        }

        public void AtualizarOrdem(int novaOrdem)
        {
            if (novaOrdem < 0)
                throw new ArgumentException("Ordem não pode ser negativa");

            Ordem = novaOrdem;
        }

        public bool TemDescendente(int categoriaId)
        {
            return SubCategorias.Any(s => s.Id == categoriaId || s.TemDescendente(categoriaId));
        }

        public bool TemCiclo()
        {
            var categoria = this;
            var visitados = new HashSet<int>();

            while (categoria?.CategoriaPaiId != null)
            {
                if (!visitados.Add(categoria.Id))
                    return true;

                categoria = categoria.CategoriaPai;
            }

            return false;
        }

        private int CalcularNivel()
        {
            int nivel = 0;
            var categoria = this;

            while (categoria.CategoriaPai != null)
            {
                nivel++;
                categoria = categoria.CategoriaPai;
            }

            return nivel;
        }

        private string GerarCaminhoCompleto()
        {
            var caminho = new List<string> { Nome };
            var categoria = this;

            while (categoria.CategoriaPai != null)
            {
                caminho.Add(categoria.CategoriaPai.Nome);
                categoria = categoria.CategoriaPai;
            }

            caminho.Reverse();
            return string.Join(" > ", caminho);
        }

        #endregion

        #region Object Overrides

        public override string ToString()
        {
            return CaminhoCompleto;
        }

        #endregion

        #region Categorias Padrão

        public static CategoriaModel[] CategoriasPadrao => new[]
        {
            new CategoriaModel 
            { 
                Nome = "Alimentos", 
                Cor = "#FF0000",
                Icone = "food",
                Ordem = 1,
                IsPadrao = true,
                SubCategorias = new List<CategoriaModel>
                {
                    new() { Nome = "Frutas", Cor = "#FF3333", Icone = "fruit", Ordem = 1, IsPadrao = true },
                    new() { Nome = "Verduras", Cor = "#FF6666", Icone = "vegetable", Ordem = 2, IsPadrao = true },
                    new() { Nome = "Carnes", Cor = "#FF9999", Icone = "meat", Ordem = 3, IsPadrao = true },
                    new() { Nome = "Laticínios", Cor = "#FFCCCC", Icone = "dairy", Ordem = 4, IsPadrao = true }
                }
            },
            new CategoriaModel 
            { 
                Nome = "Limpeza", 
                Cor = "#0000FF",
                Icone = "cleaning",
                Ordem = 2,
                IsPadrao = true
            },
            new CategoriaModel 
            { 
                Nome = "Higiene", 
                Cor = "#00FF00",
                Icone = "hygiene",
                Ordem = 3,
                IsPadrao = true
            },
            new CategoriaModel 
            { 
                Nome = "Bebidas", 
                Cor = "#FFFF00",
                Icone = "drink",
                Ordem = 4,
                IsPadrao = true
            },
            new CategoriaModel 
            { 
                Nome = "Outros", 
                Cor = "#808080",
                Icone = "other",
                Ordem = 5,
                IsPadrao = true
            }
        };

        #endregion
    }
}