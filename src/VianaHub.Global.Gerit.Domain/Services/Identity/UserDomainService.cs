using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Services.Identity;

/// <summary>
/// Serviço de domínio para operações relacionadas a usuários
/// Centraliza validações e regras de negócio
/// </summary>
public class UserDomainService : IUserDomainService
{
    private readonly IUserDataRepository _repo;
    private readonly IEntityDomainValidator<UserEntity> _validator;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;

    public UserDomainService(
        IUserDataRepository repo,
        INotify notify,
        IEntityDomainValidator<UserEntity> validator,
        ILocalizationService localization)
    {
        _repo = repo;
        _notify = notify;
        _validator = validator;
        _localization = localization;
    }

    public async Task<UserEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _repo.GetByIdAsync(id, ct);
    }
    public async Task<IEnumerable<UserEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _repo.GetAllAsync(ct);
    }
    public async Task<ListPage<UserEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        return await _repo.GetPagedAsync(request, ct);
    }
    public async Task<bool> ExistsByIdAsync(int id, CancellationToken ct)
    {
        return await _repo.ExistsByIdAsync(id, ct);
    }

    public async Task<bool> CreateAsync(UserEntity entity, CancellationToken ct)
    {
        return await ValidateCreateAsync(entity) && await _repo.CreateAsync(entity, ct);
    }

    public async Task<bool> CreateManyAsync(IReadOnlyCollection<UserEntity> entities, CancellationToken ct)
    {
        foreach (var entity in entities)
        {
            if (!await ValidateCreateAsync(entity))
                return false;
        }

        return await _repo.CreateManyAsync(entities, ct);
    }

    private async Task<bool> ValidateCreateAsync(UserEntity entity)
    {
        var validationResult = await _validator.ValidateForCreateAsync(entity);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                _notify.Add(error.ErrorMessage, 400);
            }
            return false;
        }

        return true;
    }
    public async Task<bool> UpdateAsync(UserEntity entity, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateForUpdateAsync(entity);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                _notify.Add(error.ErrorMessage, 400);
            }
            return false;
        }

        return await SaveChangesAsync(entity, ct);
    }
    public async Task<bool> ActivateAsync(UserEntity entity, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateForActivateAsync(entity);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                _notify.Add(error.ErrorMessage, 400);
            }
            return false;
        }

        return await SaveChangesAsync(entity, ct);
    }
    public async Task<bool> DeactivateAsync(UserEntity entity, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateForDeactivateAsync(entity);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                _notify.Add(error.ErrorMessage, 400);
            }
            return false;
        }

        return await SaveChangesAsync(entity, ct);
    }
    public async Task<bool> DeleteAsync(UserEntity entity, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateForDeleteAsync(entity);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                _notify.Add(error.ErrorMessage, 400);
            }
            return false;
        }

        return await SaveChangesAsync(entity, ct);
    }

    private async Task<bool> SaveChangesAsync(UserEntity entity, CancellationToken ct)
    {
        if (await _repo.UpdateAsync(entity, ct))
            return true;

        _notify.Add(_localization.GetMessage("Api.Helpers.NotifyExtensions.Error.Conflict"), 409);
        return false;
    }
}
