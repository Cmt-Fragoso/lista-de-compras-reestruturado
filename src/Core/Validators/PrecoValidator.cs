using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ListaCompras.Core.Models;
using ListaCompras.Core.Data;

namespace ListaCompras.Core.Validators
{
    /// <summary>
    /// Validador para preços
    /// </summary>
    public class PrecoValidator : IValidator<PrecoModel>
    {
        private readonly IItemRepository _itemRepository;

        public PrecoValidator(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        public async Task<IEnumerable<string>> ValidateAsync(PrecoModel preco)
        {
            var errors = new List<string>();

            // Validações básicas
            if (preco.Valor < 0)
                errors.Add("Valor não pode ser negativo");

            if (preco.Local?.Length > 200)
                errors.Add("Local não pode ter mais que 200 caracteres");

            if (preco.Observacoes?.Length > 500)
                errors.Add("Observações não podem ter mais que 500 caracteres");

            // Validação de fonte
            if (!System.Enum.IsDefined(typeof(FontePreco), preco.Fonte))
                errors.Add("Fonte do preço é inválida");

            // Validações de data
            if (preco.DataPreco > System.DateTime.Now)
                errors.Add("Data do preço não pode ser futura");

            // Validações de referências
            if (preco.ItemId != 0)
            {
                var itemExists = await _itemRepository.ExistsAsync(i => i.Id == preco.ItemId);
                if (!itemExists)
                    errors.Add("Item especificado não existe");
            }

            return errors;
        }

        public async Task<bool> IsValidAsync(PrecoModel preco)
        {
            var errors = await ValidateAsync(preco);
            return !errors.Any();
        }
    }
}