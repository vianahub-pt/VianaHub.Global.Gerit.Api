using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.ClientContact;

/// <summary>
/// Validador para exclus�o de ClientContact
/// </summary>
public class DeleteClientContactValidator : AbstractValidator<ClientContactPersonsEntity>
{
    public DeleteClientContactValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.ClientContactPerson.IdRequired"));

    }
}
