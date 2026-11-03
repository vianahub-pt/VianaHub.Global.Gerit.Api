using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.EquipmentType;

public class UpdateEquipmentTypeValidator : AbstractValidator<EquipmentTypeEntity>
{
    public UpdateEquipmentTypeValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.EquipmentType.IdRequired"));

        RuleFor(x => x.TenantId)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.EquipmentType.TenantIdRequired"));

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Domain.EquipmentType.NameRequired"))
            .MaximumLength(200)
            .WithMessage(localization.GetMessage("Domain.EquipmentType.NameMaxLength", 200));

        RuleFor(x => x.Description)
            .NotNull().WithMessage(localization.GetMessage("Domain.EquipmentType.DescriptionRequired"))
            .MaximumLength(500)
            .WithMessage(localization.GetMessage("Domain.EquipmentType.DescriptionMaxLength", 500));
    }
}
