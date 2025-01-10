using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ListaCompras.Core.Models;
using ListaCompras.Core.Data;

namespace ListaCompras.Core.Validators
{
    /// <summary>
    /// Validador para listas de compras
    /// </summary>
    public class ListaValidator : IValidator<ListaModel>
    {
        private readonly IUsuarioRepository _usuarioRepository;

        public ListaValidator(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        public async Task<IEnumerable<string>> ValidateAsync(ListaModel lista)
        {
            var errors = new List<string>();

            // Validações básicas
            if (string.IsNullOrWhiteSpace(lista.Nome))
                errors.Add("Nome da lista é obrigatório");

            if (lista.Nome?.Length > 100)
                errors.Add("Nome da lista não pode ter mais que 100 caracteres");

            if (lista.Descricao?.Length > 500)
                errors.Add("Descrição não pode ter mais que 500 caracteres");

            if (lista.OrcamentoPrevisto < 0)
                errors.Add("Orçamento previsto não pode ser negativo");

            if (lista.ValorTotal < 0)
                errors.Add("Valor total não pode ser negativo");

            // Validação de status
            if (!System.Enum.IsDefined(typeof(StatusLista), lista.Status))
                errors.Add("Status da lista é inválido");

            // Validações de referências
            if (lista.UsuarioId != 0)
            {
                var usuarioExists = await _usuarioRepository.ExistsAsync(u => u.Id == lista.UsuarioId);
                if (!usuarioExists)
                    errors.Add("Usuário especificado não existe");
            }

            // Validações de itens
            if (lista.Itens != null)
            {
                foreach (var item in lista.Itens)
                {
                    if (item.ListaId != lista.Id)
                        errors.Add($"Item {item.Id} referencia uma lista diferente");
                }
            }

            return errors;
        }

        public async Task<bool> IsValidAsync(ListaModel lista)
        {
            var errors = await ValidateAsync(lista);
            return !errors.Any();
        }
    }
}