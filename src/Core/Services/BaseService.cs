using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using ListaCompras.Core.Models;
using ListaCompras.Core.Validators;
using Microsoft.Extensions.Logging;

namespace ListaCompras.Core.Services
{
    /// <summary>
    /// Classe base para serviços
    /// </summary>
    public abstract class BaseService<T> where T : class
    {
        protected readonly IValidator<T> _validator;
        protected readonly ILogger _logger;

        protected BaseService(IValidator<T> validator, ILogger logger)
        {
            _validator = validator;
            _logger = logger;
        }

        /// <summary>
        /// Valida uma entidade e lança exceção se inválida
        /// </summary>
        protected async Task ValidateAndThrowAsync(T entity)
        {
            var errors = await _validator.ValidateAsync(entity);
            if (errors.Any())
            {
                var message = string.Join(Environment.NewLine, errors);
                _logger.LogWarning($"Validação falhou para {typeof(T).Name}: {message}");
                throw new ValidationException(message);
            }
        }

        /// <summary>
        /// Registra operação e relança exceção com contexto
        /// </summary>
        protected async Task ExecuteOperationAsync(Func<Task> operation, string operationName)
        {
            try
            {
                await operation();
                _logger.LogInformation($"Operação {operationName} concluída com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro na operação {operationName}");
                throw new ServiceException($"Erro ao executar {operationName}", ex);
            }
        }

        /// <summary>
        /// Registra operação e relança exceção com contexto (com retorno)
        /// </summary>
        protected async Task<TResult> ExecuteOperationAsync<TResult>(
            Func<Task<TResult>> operation, 
            string operationName)
        {
            try
            {
                var result = await operation();
                _logger.LogInformation($"Operação {operationName} concluída com sucesso");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro na operação {operationName}");
                throw new ServiceException($"Erro ao executar {operationName}", ex);
            }
        }
    }

    /// <summary>
    /// Exceção para erros de validação
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }

    /// <summary>
    /// Exceção para erros de serviço
    /// </summary>
    public class ServiceException : Exception
    {
        public ServiceException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}