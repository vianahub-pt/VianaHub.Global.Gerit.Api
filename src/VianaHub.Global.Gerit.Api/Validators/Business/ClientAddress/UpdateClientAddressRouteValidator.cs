using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientAddress;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Business.ClientAddress;

/// <summary>
/// Validador de rota para atualiza��o de ClientAddress
/// </summary>
public class UpdateClientAddressRouteValidator : AbstractValidator<UpdateClientAddressRequest>
{
    public UpdateClientAddressRouteValidator(ILocalizationService localization)
    {
        RuleFor(x => x.AddressTypeId)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Api.Validator.ClientAddress.Update.AddressTypeId"));

        RuleFor(x => x.Street)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Api.Validator.ClientAddress.Update.Street"))
            .MaximumLength(200)
            .WithMessage(localization.GetMessage("Api.Validator.ClientAddress.Update.Street.MaximumLength", 200));

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Api.Validator.ClientAddress.Update.City"))
            .MaximumLength(100)
            .WithMessage(localization.GetMessage("Api.Validator.ClientAddress.Update.City.MaximumLength", 100));

        RuleFor(x => x.PostalCode)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Api.Validator.ClientAddress.Update.PostalCode"))
            .MaximumLength(20)
            .WithMessage(localization.GetMessage("Api.Validator.ClientAddress.Update.PostalCode.MaximumLength", 20));

        RuleFor(x => x.District)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.District))
            .WithMessage(localization.GetMessage("Api.Validator.ClientAddress.Update.District.MaximumLength", 100));

        RuleFor(x => x.Neighborhood)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Neighborhood))
            .WithMessage(localization.GetMessage("Api.Validator.ClientAddress.Update.Neighborhood.MaximumLength", 100));

        RuleFor(x => x.StreetNumber)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.StreetNumber))
            .WithMessage(localization.GetMessage("Api.Validator.ClientAddress.Update.StreetNumber.MaximumLength", 20));

        RuleFor(x => x.Complement)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Complement))
            .WithMessage(localization.GetMessage("Api.Validator.ClientAddress.Update.Complement.MaximumLength", 100));

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Note))
            .WithMessage(localization.GetMessage("Api.Validator.ClientAddress.Update.Notes.MaximumLength", 500));

        RuleFor(x => x.CountryCode)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Api.Validator.ClientAddress.Update.CountryCode"))
            .Length(2)
            .WithMessage(localization.GetMessage("Api.Validator.ClientAddress.Update.CountryCode.Length"));

        RuleFor(x => x)
            .Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
            .WithMessage(localization.GetMessage("Domain.ClientAddress.CoordinatesRequiredTogether"));
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue)
            .WithMessage(localization.GetMessage("Domain.ClientAddress.LatitudeRange"));
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue)
            .WithMessage(localization.GetMessage("Domain.ClientAddress.LongitudeRange"));
    }
}
