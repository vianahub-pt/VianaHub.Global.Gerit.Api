using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.ClientAddress;

/// <summary>
/// Validador para atualiza��o de ClientAddress
/// </summary>
public class UpdateClientAddressValidator : AbstractValidator<ClientAddressesEntity>
{
    public UpdateClientAddressValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.ClientAddress.IdRequired"));

        RuleFor(x => x.AddressTypeId)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.ClientAddress.AddressTypeIdRequired"));

        RuleFor(x => x.CountryCode)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Domain.ClientAddress.CountryCodeRequired"))
            .Length(2)
            .WithMessage(localization.GetMessage("Domain.ClientAddress.CountryCodeLength"));

        RuleFor(x => x.Street)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Domain.ClientAddress.StreetRequired"))
            .MaximumLength(200)
            .WithMessage(localization.GetMessage("Domain.ClientAddress.StreetMaxLength"));

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Domain.ClientAddress.CityRequired"))
            .MaximumLength(100)
            .WithMessage(localization.GetMessage("Domain.ClientAddress.CityMaxLength"));

        RuleFor(x => x.PostalCode)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Domain.ClientAddress.PostalCodeRequired"))
            .MaximumLength(20)
            .WithMessage(localization.GetMessage("Domain.ClientAddress.PostalCodeMaxLength"));

        RuleFor(x => x.District)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.District))
            .WithMessage(localization.GetMessage("Domain.ClientAddress.DistrictMaxLength"));

        RuleFor(x => x.Neighborhood)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Neighborhood))
            .WithMessage(localization.GetMessage("Domain.ClientAddress.NeighborhoodMaxLength"));

        RuleFor(x => x.StreetNumber)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.StreetNumber))
            .WithMessage(localization.GetMessage("Domain.ClientAddress.StreetNumberMaxLength"));

        RuleFor(x => x.Complement)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Complement))
            .WithMessage(localization.GetMessage("Domain.ClientAddress.ComplementMaxLength"));

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Note))
            .WithMessage(localization.GetMessage("Domain.ClientAddress.NotesMaxLength"));

        RuleFor(x => x.IsDeleted)
            .Equal(false)
            .WithMessage(localization.GetMessage("Domain.ClientAddress.CannotUpdateDeleted"));

        RuleFor(x => x.ModifiedBy)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.ClientAddress.ModifiedByRequired"));

        RuleFor(x => x)
            .Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
            .WithMessage(localization.GetMessage("Domain.ClientAddress.CoordinatesRequiredTogether"));
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .When(x => x.Latitude.HasValue)
            .WithMessage(localization.GetMessage("Domain.ClientAddress.LatitudeRange"));
        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .When(x => x.Longitude.HasValue)
            .WithMessage(localization.GetMessage("Domain.ClientAddress.LongitudeRange"));
    }
}
