using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Services.Identity;

public class ResourceDomainService : IResourceDomainService
{
    private readonly IResourceDataRepository _repo;
    private readonly IEntityDomainValidator<ResourceEntity> _validator;
    private readonly INotify _notify;

    public ResourceDomainService(
        IResourceDataRepository repo,
        IEntityDomainValidator<ResourceEntity> validator,
        INotify notify)
    {
        _repo = repo;
        _validator = validator;
        _notify = notify;
    }

    public async Task<ResourceEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _repo.GetByIdAsync(id, ct);
    }
    public async Task<IEnumerable<ResourceEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _repo.GetAllAsync(ct);
    }
    public async Task<ListPage<ResourceEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        return await _repo.GetPagedAsync(request, ct);
    }
    public async Task<bool> ExistsByIdAsync(int id, CancellationToken ct)
    {
        return await _repo.ExistsByIdAsync(id, ct);
    }
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
    {
        return await _repo.ExistsByNameAsync(name, ct);
    }

    public async Task<bool> CreateAsync(ResourceEntity entity, CancellationToken ct)
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

        return await _repo.AddAsync(entity, ct);
    }
    public async Task<bool> UpdateAsync(ResourceEntity entity, CancellationToken ct)
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

        return await _repo.UpdateAsync(entity, ct);
    }
    public async Task<bool> ActivateAsync(ResourceEntity entity, CancellationToken ct)
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

        return await _repo.UpdateAsync(entity, ct);
    }
    public async Task<bool> DeactivateAsync(ResourceEntity entity, CancellationToken ct)
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

        return await _repo.UpdateAsync(entity, ct);
    }
    public async Task<bool> DeleteAsync(ResourceEntity entity, CancellationToken ct)
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

        return await _repo.UpdateAsync(entity, ct);
    }
}
