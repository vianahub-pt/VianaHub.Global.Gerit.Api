using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.PartyType;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Business.PartyType;

/// <summary>
/// Validador de rota para atualização de PartyType.
/// </summary>
public class UpdatePartyTypeRouteValidator : AbstractValidator<UpdatePartyTypeRequest>
{
    public UpdatePartyTypeRouteValidator(ILocalizationService localization)
    {

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Api.Validator.PartyType.Update.Name"))
            .MaximumLength(100)
            .WithMessage(localization.GetMessage("Api.Validator.PartyType.Update.Name.MaximumLength", 100));
    }
}
