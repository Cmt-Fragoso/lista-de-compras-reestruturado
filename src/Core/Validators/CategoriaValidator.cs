using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using ListaCompras.Core.Models;
using ListaCompras.Core.Data;

namespace ListaCompras.Core.Validators
{
    /// <summary>
    /// Validador para categorias
    /// </summary>
    public class CategoriaValidator : IValidator<CategoriaModel>
    {
        private readonly ICategoriaRepository _categoriaRepository;

        public CategoriaValidator(ICategoriaRepository categoriaRepository)
        {
            _categoriaRepository = categoriaRepository;
        }

        public async Task<IEnumerable<string>> ValidateAsync(CategoriaModel categoria)
        {
            var errors = new List<string>();

            // Validações básicas
            if (string.IsNullOrWhiteSpace(categoria.Nome))
                errors.Add("Nome da categoria é obrigatório");

            if (categoria.Nome?.Length > 50)
                errors.Add("Nome da categoria não pode ter mais que 50 caracteres");

            if (categoria.Descricao?.Length > 200)
                errors.Add("Descrição não pode ter mais que 200 caracteres");

            // Validação de cor em formato hex
            if (!string.IsNullOrWhiteSpace(categoria.Cor))
            {
                if (!Regex.IsMatch(categoria.Cor, "^#[0-9A-Fa-f]{6}$"))
                    errors.Add("Cor deve estar no formato hexadecimal (#RRGGBB)");
            }

            if (categoria.Icone?.Length > 50)
                errors.Add("Nome do ícone não pode ter mais que 50 caracteres");

            if (categoria.Ordem < 0)
                errors.Add("Ordem não pode ser negativa");

            // Validação de hierarquia
            if (categoria.CategoriaPaiId.HasValue)
            {
                // Verifica se categoria pai existe
                var categoriaPaiExists = await _categoriaRepository.ExistsAsync(c => c.Id == categoria.CategoriaPaiId);
                if (!categoriaPaiExists)
                    errors.Add("Categoria pai especificada não existe");

                // Evita ciclos na hierarquia
                if (categoria.Id != 0) // Se não é uma nova categoria
                {
                    var subcategorias = await _categoriaRepository.GetSubcategoriasAsync(categoria.Id);
                    if (subcategorias.Any(c => c.Id == categoria.CategoriaPaiId))
                        errors.Add("Uma categoria não pode ser subcategoria de si mesma");
                }
            }

            return errors;
        }

        public async Task<bool> IsValidAsync(CategoriaModel categoria)
        {
            var errors = await ValidateAsync(categoria);
            return !errors.Any();
        }
    }
}