using VianaHub.Global.Gerit.Domain.Helpers;
using FluentValidation;
using System.Net;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Filters;

/// <summary>
/// Filtro para validação automática de requests usando FluentValidation.
/// Integrado com INotify para capturar erros de validação.
/// </summary>
public class ValidationFilter<TRequest> : IEndpointFilter where TRequest : class
{
    private readonly IValidator<TRequest>? _validator;
    private readonly ILogger<ValidationFilter<TRequest>> _logger;

    /// <summary>
    /// Inicializa uma nova instância do <see cref="ValidationFilter{TRequest}"/>.
    /// </summary>
    public ValidationFilter(IServiceProvider serviceProvider, ILogger<ValidationFilter<TRequest>> logger)
    {
        _validator = serviceProvider.GetService<IValidator<TRequest>>();
        _logger = logger;
    }

    /// <summary>
    /// Executa a validação do request e retorna erros agrupados, se houver.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        _logger.LogDebug("ValidationFilter<{RequestType}> executando", typeof(TRequest).Name);

        if (_validator == null)
        {
            _logger.LogWarning("Nenhum validador registrado para {RequestType}", typeof(TRequest).Name);
            return await next(context);
        }

        var notify = context.HttpContext.RequestServices.GetService<INotify>();
        TRequest? request = null;

        _logger.LogDebug("Total de argumentos: {Count}", context.Arguments.Count);
        for (int i = 0; i < context.Arguments.Count; i++)
        {
            var arg = context.Arguments[i];
            if (arg != null)
            {
                var argType = arg.GetType().Name;
                _logger.LogDebug("Argumento[{Index}]: {Type}", i, argType);
                if (arg is TRequest typedRequest)
                {
                    request = typedRequest;
                    _logger.LogDebug("Request encontrado no argumento {Index}", i);
                    break;
                }
            }
        }

        if (request == null)
        {
            _logger.LogWarning("Request do tipo {RequestType} não encontrado nos argumentos", typeof(TRequest).Name);
            return await next(context);
        }

        _logger.LogInformation("Validando request do tipo {RequestType}", typeof(TRequest).Name);
        var validationResult = await _validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validação falhou para {RequestType}. Erros: {ErrorCount}",
                typeof(TRequest).Name, validationResult.Errors.Count);

            if (notify != null)
            {
                foreach (var error in validationResult.Errors)
                {
                    notify.Add($"{error.PropertyName}: {error.ErrorMessage}", 400);
                    _logger.LogDebug("Erro de validação adicionado ao Notify: {PropertyName} - {ErrorMessage}", LogValueSanitizer.Sanitize(error.PropertyName), LogValueSanitizer.Sanitize(error.ErrorMessage));
                }
            }

            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return Results.BadRequest(new
            {
                title = "Erro de validação",
                errors = errors
            });
        }

        _logger.LogInformation("Validação passou para {RequestType}", typeof(TRequest).Name);
        return await next(context);
    }
}
