using System;
using System.ComponentModel.DataAnnotations;

namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Representa uma categoria de itens
    /// </summary>
    public class CategoriaModel
    {
        /// <summary>
        /// Identificador único da categoria
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome da categoria
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Nome { get; set; }

        /// <summary>
        /// Descrição da categoria
        /// </summary>
        [StringLength(200)]
        public string Descricao { get; set; }

        /// <summary>
        /// Cor para representação visual (em hex: #RRGGBB)
        /// </summary>
        [StringLength(7)]
        [RegularExpression("^#[0-9A-Fa-f]{6}$")]
        public string Cor { get; set; }

        /// <summary>
        /// Ícone para representação visual
        /// </summary>
        [StringLength(50)]
        public string Icone { get; set; }

        /// <summary>
        /// Ordem de exibição da categoria
        /// </summary>
        public int Ordem { get; set; }

        /// <summary>
        /// ID da categoria pai (para subcategorias)
        /// </summary>
        public int? CategoriaPaiId { get; set; }

        /// <summary>
        /// Data de criação
        /// </summary>
        public DateTime DataCriacao { get; set; }

        /// <summary>
        /// Data da última atualização
        /// </summary>
        public DateTime DataAtualizacao { get; set; }

        /// <summary>
        /// Versão do registro para controle de concorrência
        /// </summary>
        public byte[] Version { get; set; }
    }
}