using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.DocumentType;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Business.DocumentType;

/// <summary>
/// Validador de rota para atualização de DocumentType.
/// </summary>
public class UpdateDocumentTypeRouteValidator : AbstractValidator<UpdateDocumentTypeRequest>
{
    public UpdateDocumentTypeRouteValidator(ILocalizationService localization)
    {
    }
}
