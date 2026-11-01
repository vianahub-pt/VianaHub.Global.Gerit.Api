using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.AcquisitionSourceType;

public class DeleteAcquisitionSourceTypeValidator : AbstractValidator<AcquisitionSourceTypeEntity>
{
    public DeleteAcquisitionSourceTypeValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage(localization.GetMessage("Domain.AcquisitionSourceType.IdRequired"));
    }
}
