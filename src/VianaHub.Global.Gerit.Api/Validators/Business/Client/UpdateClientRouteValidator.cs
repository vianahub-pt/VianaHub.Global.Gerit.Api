using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Client;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Business.Client;

/// <summary>
/// Validador para UpdateClientRequest
/// </summary>
public class UpdateClientRouteValidator : AbstractValidator<UpdateClientRequest>
{
    public UpdateClientRouteValidator(ILocalizationService localization)
    {
        RuleFor(x => x.PartyTypeId)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Api.Validator.Client.Update.PartyTypeId"));

        RuleFor(x => x.AcquisitionSourceTypeId)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Api.Validator.Client.Update.AcquisitionSourceTypeId"));

        RuleFor(x => x.Name).NotEmpty().MaximumLength(500)
            .WithMessage(localization.GetMessage("Api.Validator.Client.Update.Name"));
        RuleFor(x => x.StatusDefinitionId).GreaterThan(0)
            .WithMessage(localization.GetMessage("Api.Validator.Client.Update.StatusDefinitionId"));
        RuleFor(x => x.StatusDomainId).GreaterThan(0)
            .WithMessage(localization.GetMessage("Api.Validator.Client.Update.StatusDomainId"));
    }
}
