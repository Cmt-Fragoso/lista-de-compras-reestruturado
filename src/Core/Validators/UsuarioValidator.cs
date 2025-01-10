using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using ListaCompras.Core.Models;
using ListaCompras.Core.Data;

namespace ListaCompras.Core.Validators
{
    /// <summary>
    /// Validador para usuários
    /// </summary>
    public class UsuarioValidator : IValidator<UsuarioModel>
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public UsuarioValidator(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        public async Task<IEnumerable<string>> ValidateAsync(UsuarioModel usuario)
        {
            var errors = new List<string>();

            // Validações básicas
            if (string.IsNullOrWhiteSpace(usuario.Nome))
                errors.Add("Nome do usuário é obrigatório");

            if (usuario.Nome?.Length > 100)
                errors.Add("Nome do usuário não pode ter mais que 100 caracteres");

            if (string.IsNullOrWhiteSpace(usuario.Email))
                errors.Add("Email é obrigatório");

            if (usuario.Email?.Length > 255)
                errors.Add("Email não pode ter mais que 255 caracteres");

            // Validação de formato de email
            if (!string.IsNullOrWhiteSpace(usuario.Email) && !EmailRegex.IsMatch(usuario.Email))
                errors.Add("Email está em formato inválido");

            if (string.IsNullOrWhiteSpace(usuario.SenhaHash))
                errors.Add("Senha é obrigatória");

            if (string.IsNullOrWhiteSpace(usuario.DispositivoId))
                errors.Add("ID do dispositivo é obrigatório");

            if (usuario.DispositivoId?.Length > 100)
                errors.Add("ID do dispositivo não pode ter mais que 100 caracteres");

            // Validação de status
            if (!System.Enum.IsDefined(typeof(StatusUsuario), usuario.Status))
                errors.Add("Status do usuário é inválido");

            // Validação de unicidade de email
            if (!string.IsNullOrWhiteSpace(usuario.Email))
            {
                var emailExists = await _usuarioRepository.EmailExisteAsync(usuario.Email);
                if (emailExists && usuario.Id == 0) // Apenas para novos usuários
                    errors.Add("Email já está em uso");
            }

            // Validação do formato JSON das preferências
            if (!string.IsNullOrWhiteSpace(usuario.Preferencias))
            {
                try
                {
                    JsonDocument.Parse(usuario.Preferencias);
                }
                catch (JsonException)
                {
                    errors.Add("Preferências devem estar em formato JSON válido");
                }
            }

            // Validação de datas
            if (usuario.UltimoAcesso.HasValue && usuario.UltimoAcesso > System.DateTime.Now)
                errors.Add("Data do último acesso não pode ser futura");

            return errors;
        }

        public async Task<bool> IsValidAsync(UsuarioModel usuario)
        {
            var errors = await ValidateAsync(usuario);
            return !errors.Any();
        }
    }
}