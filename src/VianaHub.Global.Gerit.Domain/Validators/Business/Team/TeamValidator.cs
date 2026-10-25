using FluentValidation.Results;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.Team;

public class TeamValidator : BaseEntityValidator<TeamEntity>
{

    public TeamValidator(ILocalizationService localization) : base(localization)
    {
    }

    public override async Task<ValidationResult> ValidateForCreateAsync(TeamEntity entity)
    {
        var validator = new CreateTeamValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForUpdateAsync(TeamEntity entity)
    {
        var validator = new UpdateTeamValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForActivateAsync(TeamEntity entity)
    {
        var validator = new ActivateTeamValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeactivateAsync(TeamEntity entity)
    {
        var validator = new DeactivateTeamValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeleteAsync(TeamEntity entity)
    {
        var validator = new DeleteTeamValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override Task<ValidationResult> ValidateForRevokeAsync(TeamEntity entity)
    {
        return Task.FromResult(new ValidationResult());
    }
}
