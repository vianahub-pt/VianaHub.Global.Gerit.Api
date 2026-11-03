using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.EquipmentType;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Business.EquipmentType;

public class CreateEquipmentTypeRouteValidator : AbstractValidator<CreateEquipmentTypeRequest>
{
    public CreateEquipmentTypeRouteValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Api.EquipmentType.NameRequired"))
            .MaximumLength(200)
            .WithMessage(localization.GetMessage("Api.EquipmentType.NameMaxLength", 200));

        RuleFor(x => x.Description)
            .NotNull().WithMessage(localization.GetMessage("Api.EquipmentType.DescriptionRequired"))
            .MaximumLength(500)
            .WithMessage(localization.GetMessage("Api.EquipmentType.DescriptionMaxLength", 500));
    }
}
