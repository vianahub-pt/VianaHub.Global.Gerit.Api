using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.Visit;

public class UpdateVisitValidator : AbstractValidator<VisitEntity>
{
    public UpdateVisitValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.Visit.IdRequired"));

        RuleFor(x => x.ClientId)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.Visit.ClientIdRequired"));

        RuleFor(x => x.StatusDefinitionId)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.Visit.StatusDefinitionIdRequired"));

        RuleFor(x => x.StatusDomainId)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.Visit.StatusDomainIdRequired"));

        RuleFor(x => x.CurrencyCode)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Domain.Visit.CurrencyCodeRequired"))
            .MaximumLength(3)
            .WithMessage(localization.GetMessage("Domain.Visit.CurrencyCodeMaxLength", 3));

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Domain.Visit.TitleRequired"))
            .MaximumLength(200)
            .WithMessage(localization.GetMessage("Domain.Visit.TitleMaxLength", 200));

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Domain.Visit.DescriptionRequired"))
            .MaximumLength(2000)
            .WithMessage(localization.GetMessage("Domain.Visit.DescriptionMaxLength", 2000));

        RuleFor(x => x.StartDateTime)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Domain.Visit.StartDateTimeRequired"));

        RuleFor(x => x.EstimatedValue)
            .GreaterThanOrEqualTo(0)
            .WithMessage(localization.GetMessage("Domain.Visit.EstimatedValueGreaterThanOrEqual"));

        When(x => x.RealValue.HasValue, () =>
        {
            RuleFor(x => x.RealValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage(localization.GetMessage("Domain.Visit.RealValueGreaterThanOrEqual"));
        });

        When(x => x.EndDateTime.HasValue, () =>
        {
            RuleFor(x => x.EndDateTime)
                .GreaterThan(x => x.StartDateTime)
                .WithMessage(localization.GetMessage("Domain.Visit.EndDateTimeGreaterThanStart"));
        });

        RuleFor(x => x.IsDeleted)
            .Must(x => !x)
            .WithMessage(localization.GetMessage("Domain.Visit.CannotUpdateDeleted"));
    }
}
