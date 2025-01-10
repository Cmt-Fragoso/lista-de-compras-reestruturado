using System;

namespace ListaCompras.Core.Models
{
    public class PrecoModel
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public ItemModel Item { get; set; }
        public decimal Valor { get; set; }
        public string Local { get; set; }
        public DateTime Data { get; set; }
        public string Observacao { get; set; }
        public bool IsPromocional { get; set; }
        public DateTime? DataFimPromocao { get; set; }
        public FontePreco Fonte { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime DataAtualizacao { get; set; }
        public string Observacoes { get; set; }
        public int Version { get; set; }
    }
}
