using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.AcquisitionSourceType;

public class DeactivateAcquisitionSourceTypeValidator : AbstractValidator<AcquisitionSourceTypeEntity>
{
    public DeactivateAcquisitionSourceTypeValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage(localization.GetMessage("Domain.AcquisitionSourceType.IdRequired"));
    }
}
