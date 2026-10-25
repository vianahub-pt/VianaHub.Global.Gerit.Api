using FluentValidation;
using System.Net;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Filters;

/// <summary>
/// Filtro para validação de upload de arquivo de Actions.
/// </summary>
public class FileValidationFilter : IEndpointFilter
{
    private readonly IValidator<ImportActionFileRequest> _validator;
    private readonly ILogger<FileValidationFilter> _logger;

    /// <summary>
    /// Inicializa uma nova instância do <see cref="FileValidationFilter"/>.
    /// </summary>
    public FileValidationFilter(
        IServiceProvider serviceProvider,
        ILogger<FileValidationFilter> logger)
    {
        _validator = serviceProvider.GetService<IValidator<ImportActionFileRequest>>()!;
        _logger = logger;
    }

    /// <summary>
    /// Executa a validação do arquivo antes de processar o request.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        _logger.LogDebug("FileValidationFilter executando");

        if (_validator == null)
        {
            _logger.LogWarning("Nenhum validador registrado para ImportActionFileRequest");
            return await next(context);
        }

        var httpContext = context.HttpContext;
        var notify = httpContext.RequestServices.GetService<INotify>();

        // Criar request para validação
        var request = new ImportActionFileRequest
        {
            ContentType = httpContext.Request.ContentType,
            File = null
        };

        // Tentar obter o arquivo do form
        if (httpContext.Request.HasFormContentType)
        {
            try
            {
                var form = await httpContext.Request.ReadFormAsync();
                request.File = form.Files.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao ler arquivo do form");
                if (notify != null)
                {
                    notify.Add($"Erro ao processar arquivo: {ex.Message}", 400);
                }
                return Results.BadRequest(new
                {
                    title = "Erro ao processar arquivo",
                    message = "Não foi possível ler o arquivo enviado"
                });
            }
        }

        _logger.LogInformation("Validando upload de arquivo");
        var validationResult = await _validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validação falhou. Erros: {ErrorCount}", validationResult.Errors.Count);

            if (notify != null)
            {
                foreach (var error in validationResult.Errors)
                {
                    notify.Add(error.ErrorMessage, 400);
                    _logger.LogDebug("Erro de validação: {ErrorMessage}", error.ErrorMessage);
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

        _logger.LogInformation("Validação de arquivo passou");
        return await next(context);
    }
}
