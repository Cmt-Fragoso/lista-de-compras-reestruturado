using System;
using System.ComponentModel.DataAnnotations;

namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Representa um usuário do sistema
    /// </summary>
    public class UsuarioModel
    {
        /// <summary>
        /// Identificador único do usuário
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome do usuário
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        /// <summary>
        /// Email do usuário
        /// </summary>
        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// Hash da senha do usuário
        /// </summary>
        [Required]
        public string SenhaHash { get; set; }

        /// <summary>
        /// ID do dispositivo principal do usuário
        /// </summary>
        [Required]
        [StringLength(100)]
        public string DispositivoId { get; set; }

        /// <summary>
        /// Preferências do usuário em formato JSON
        /// </summary>
        public string Preferencias { get; set; }

        /// <summary>
        /// Status do usuário no sistema
        /// </summary>
        public StatusUsuario Status { get; set; }

        /// <summary>
        /// Último acesso do usuário
        /// </summary>
        public DateTime? UltimoAcesso { get; set; }

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

    /// <summary>
    /// Status possíveis de um usuário
    /// </summary>
    public enum StatusUsuario
    {
        Ativo = 0,
        Inativo = 1,
        Bloqueado = 2,
        PendenteConfirmacao = 3
    }
}