using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Domain.Services.Identity;

public class UserRoleDomainService : IUserRoleDomainService
{
    private readonly IUserRoleDataRepository _repository;
    private readonly IValidator<UserRolesEntity> _validator;
    private readonly INotify _notify;

    public UserRoleDomainService(IUserRoleDataRepository repository, IValidator<UserRolesEntity> validator, INotify notify)
    {
        _repository = repository;
        _validator = validator;
        _notify = notify;
    }

    public async Task<IList<UserRolesEntity>> GetAllAsync(int tenantId, CancellationToken ct)
    {
        return await _repository.GetAllAsync(tenantId, ct);
    }
    public async Task<UserRolesEntity?> GetByIdAsync(int tenantId, int userId, int roleId, CancellationToken ct)
    {
        return await _repository.GetByIdAsync(tenantId, userId, roleId, ct);
    }
    public async Task<IList<UserRolesEntity>> GetByUserAsync(int tenantId, int userId, CancellationToken ct)
    {
        return await _repository.GetByUserAsync(tenantId, userId, ct);
    }
    public async Task<IList<UserRolesEntity>> GetByRoleAsync(int tenantId, int roleId, CancellationToken ct)
    {
        return await _repository.GetByRoleAsync(tenantId, roleId, ct);
    }
    public async Task<bool> ExistsAsync(int tenantId, int userId, int roleId, CancellationToken ct)
    {
        return await _repository.ExistsAsync(tenantId, userId, roleId, ct);
    }

    public async Task<bool> CreateAsync(UserRolesEntity entity, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(entity, ct);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                _notify.Add(error.ErrorMessage, 400);
            return false;
        }

        return await _repository.CreateAsync(entity, ct);
    }
    public async Task<bool> DeleteAsync(int tenantId, int userId, int roleId, CancellationToken ct)
    {
        return await _repository.DeleteAsync(tenantId, userId, roleId, ct);
    }
}
