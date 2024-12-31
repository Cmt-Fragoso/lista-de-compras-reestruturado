using System;
using System.ComponentModel.DataAnnotations;

namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Representa o histórico de preços de um item
    /// </summary>
    public class PrecoModel
    {
        /// <summary>
        /// Identificador único do registro de preço
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ID do item relacionado
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        /// Valor do preço registrado
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal Valor { get; set; }

        /// <summary>
        /// Data da coleta/registro do preço
        /// </summary>
        public DateTime DataPreco { get; set; }

        /// <summary>
        /// Local onde o preço foi coletado/registrado
        /// </summary>
        [StringLength(200)]
        public string Local { get; set; }

        /// <summary>
        /// Observações sobre o preço
        /// </summary>
        [StringLength(500)]
        public string Observacoes { get; set; }

        /// <summary>
        /// Fonte do preço (Manual, ImportadoAPI, etc)
        /// </summary>
        public FontePreco Fonte { get; set; }

        /// <summary>
        /// Flag indicando se é um preço promocional
        /// </summary>
        public bool Promocional { get; set; }

        /// <summary>
        /// Data de criação do registro
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

    /// <summary>
    /// Fontes possíveis de um preço
    /// </summary>
    public enum FontePreco
    {
        Manual = 0,
        ImportadoAPI = 1,
        Sincronizado = 2,
        Scanner = 3
    }
}