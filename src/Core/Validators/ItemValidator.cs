using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ListaCompras.Core.Models;
using ListaCompras.Core.Data;

namespace ListaCompras.Core.Validators
{
    /// <summary>
    /// Validador para itens de compra
    /// </summary>
    public class ItemValidator : IValidator<ItemModel>
    {
        private readonly ICategoriaRepository _categoriaRepository;
        private readonly IListaRepository _listaRepository;

        public ItemValidator(ICategoriaRepository categoriaRepository, IListaRepository listaRepository)
        {
            _categoriaRepository = categoriaRepository;
            _listaRepository = listaRepository;
        }

        public async Task<IEnumerable<string>> ValidateAsync(ItemModel item)
        {
            var errors = new List<string>();

            // Validações básicas
            if (string.IsNullOrWhiteSpace(item.Nome))
                errors.Add("Nome do item é obrigatório");

            if (item.Nome?.Length > 100)
                errors.Add("Nome do item não pode ter mais que 100 caracteres");

            if (item.Descricao?.Length > 500)
                errors.Add("Descrição não pode ter mais que 500 caracteres");

            if (item.Quantidade <= 0)
                errors.Add("Quantidade deve ser maior que zero");

            if (string.IsNullOrWhiteSpace(item.Unidade))
                errors.Add("Unidade de medida é obrigatória");

            if (item.Unidade?.Length > 10)
                errors.Add("Unidade de medida não pode ter mais que 10 caracteres");

            if (item.PrecoEstimado < 0)
                errors.Add("Preço estimado não pode ser negativo");

            // Validações de referências
            if (item.CategoriaId != 0)
            {
                var categoriaExists = await _categoriaRepository.ExistsAsync(c => c.Id == item.CategoriaId);
                if (!categoriaExists)
                    errors.Add("Categoria especificada não existe");
            }

            if (item.ListaId != 0)
            {
                var listaExists = await _listaRepository.ExistsAsync(l => l.Id == item.ListaId);
                if (!listaExists)
                    errors.Add("Lista especificada não existe");
            }

            return errors;
        }

        public async Task<bool> IsValidAsync(ItemModel item)
        {
            var errors = await ValidateAsync(item);
            return !errors.Any();
        }
    }
}