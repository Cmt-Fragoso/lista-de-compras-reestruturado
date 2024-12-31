using System;
using System.ComponentModel.DataAnnotations;

namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Representa um item de uma lista de compras
    /// </summary>
    public class ItemModel
    {
        /// <summary>
        /// Identificador único do item
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome do item
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        /// <summary>
        /// Descrição adicional do item
        /// </summary>
        [StringLength(500)]
        public string Descricao { get; set; }

        /// <summary>
        /// Quantidade necessária do item
        /// </summary>
        [Range(0.01, double.MaxValue)]
        public decimal Quantidade { get; set; }

        /// <summary>
        /// Unidade de medida (kg, un, l, etc)
        /// </summary>
        [Required]
        [StringLength(10)]
        public string Unidade { get; set; }

        /// <summary>
        /// Preço estimado/último preço conhecido
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal PrecoEstimado { get; set; }

        /// <summary>
        /// ID da categoria do item
        /// </summary>
        public int CategoriaId { get; set; }

        /// <summary>
        /// ID da lista a qual o item pertence
        /// </summary>
        public int ListaId { get; set; }

        /// <summary>
        /// Flag indicando se o item já foi comprado
        /// </summary>
        public bool Comprado { get; set; }

        /// <summary>
        /// Data de criação do item
        /// </summary>
        public DateTime DataCriacao { get; set; }

        /// <summary>
        /// Data da última atualização do item
        /// </summary>
        public DateTime DataAtualizacao { get; set; }

        /// <summary>
        /// Versão do registro para controle de concorrência
        /// </summary>
        public byte[] Version { get; set; }
    }
}