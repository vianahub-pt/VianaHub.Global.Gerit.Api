using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.Client;

/// <summary>
/// Validador para atualiza��o de Client
/// </summary>
public class UpdateClientValidator : AbstractValidator<ClientEntity>
{
    public UpdateClientValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.Client.IdRequired"));

        RuleFor(x => x.TenantId)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.Client.TenantIdRequired"));

        RuleFor(x => x.ModifiedBy)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.Client.ModifiedByRequired"));

        RuleFor(x => x.IsDeleted)
            .Equal(false)
            .WithMessage(localization.GetMessage("Domain.Client.CannotUpdateDeleted"));

        RuleFor(x => x.AcquisitionSourceTypeId)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.Client.AcquisitionSourceTypeIdRequired"));

        RuleFor(x => x.PartyTypeId).GreaterThan((byte)0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.StatusDefinitionId).GreaterThan(0);
        RuleFor(x => x.StatusDomainId).GreaterThan(0);
        RuleFor(x => x.BirthDate).LessThanOrEqualTo(DateTime.UtcNow.Date).When(x => x.BirthDate.HasValue);
        RuleFor(x => x.NumberOfEmployees).GreaterThanOrEqualTo(0).When(x => x.NumberOfEmployees.HasValue);

        // CK_Clients_PartyTypeData: When PartyTypeId=1 (individual), company fields must be null
        RuleFor(x => x.CompanyRegistrationNumber)
            .Null()
            .WithMessage(localization.GetMessage("Domain.Client.PartyTypeIndividualCompanyFieldsMustBeNull"))
            .When(x => x.PartyTypeId == 1);

        RuleFor(x => x.EconomicActivityCode)
            .Null()
            .WithMessage(localization.GetMessage("Domain.Client.PartyTypeIndividualCompanyFieldsMustBeNull"))
            .When(x => x.PartyTypeId == 1);

        RuleFor(x => x.NumberOfEmployees)
            .Null()
            .WithMessage(localization.GetMessage("Domain.Client.PartyTypeIndividualCompanyFieldsMustBeNull"))
            .When(x => x.PartyTypeId == 1);
    }
}
