using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;

namespace VianaHub.Global.Gerit.Domain.Services.Identity;

public class UserRoleDomainService : IUserRoleDomainService
{
    private readonly IUserRoleDataRepository _repository;
    private readonly IValidator<UserRolesEntity> _validator;

    public UserRoleDomainService(IUserRoleDataRepository repository, IValidator<UserRolesEntity> validator)
    {
        _repository = repository;
        _validator = validator;
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
        var validation = _validator.Validate(entity);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        await _repository.CreateAsync(entity, ct);
        return true;
    }
    public async Task<bool> DeleteAsync(int tenantId, int userId, int roleId, CancellationToken ct)
    {
        return await _repository.DeleteAsync(tenantId, userId, roleId, ct);
    }
}
