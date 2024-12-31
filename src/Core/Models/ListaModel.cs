using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Representa uma lista de compras
    /// </summary>
    public class ListaModel
    {
        /// <summary>
        /// Identificador único da lista
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome/título da lista
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        /// <summary>
        /// Descrição da lista
        /// </summary>
        [StringLength(500)]
        public string Descricao { get; set; }

        /// <summary>
        /// ID do usuário dono da lista
        /// </summary>
        public int UsuarioId { get; set; }

        /// <summary>
        /// Data prevista para a compra
        /// </summary>
        public DateTime? DataPrevista { get; set; }

        /// <summary>
        /// Orçamento total previsto
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal OrcamentoPrevisto { get; set; }

        /// <summary>
        /// Valor total real (após compras)
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal ValorTotal { get; set; }

        /// <summary>
        /// Status da lista (Ativa, Concluída, etc)
        /// </summary>
        [Required]
        public StatusLista Status { get; set; }

        /// <summary>
        /// Lista de itens
        /// </summary>
        public virtual ICollection<ItemModel> Itens { get; set; }

        /// <summary>
        /// Data de criação da lista
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
    /// Status possíveis de uma lista
    /// </summary>
    public enum StatusLista
    {
        EmEdicao = 0,
        Ativa = 1,
        EmCompra = 2,
        Concluida = 3,
        Arquivada = 4
    }
}