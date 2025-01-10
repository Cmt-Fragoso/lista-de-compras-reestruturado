using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ListaCompras.Core.Exceptions
{
    public class ValidationException : Exception
    {
        public IList<ValidationResult> ValidationResults { get; }

        public ValidationException(string message) : base(message)
        {
            ValidationResults = new List<ValidationResult>
            {
                new ValidationResult(message)
            };
        }

        public ValidationException(IList<ValidationResult> validationResults)
            : base("Erros de validação encontrados")
        {
            ValidationResults = validationResults;
        }

        public ValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
            ValidationResults = new List<ValidationResult>
            {
                new ValidationResult(message)
            };
        }
    }
}