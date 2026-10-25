using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDefinition;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Business.StatusDefinition;

/// <summary>
/// Validador de rota para atualização de StatusDefinition.
/// </summary>
public class UpdateStatusDefinitionRouteValidator : AbstractValidator<UpdateStatusDefinitionRequest>
{
    public UpdateStatusDefinitionRouteValidator(ILocalizationService localization)
    {

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage(localization.GetMessage("Api.Validator.StatusDefinition.Update.DisplayOrder"));
    }
}
