using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Services.Business;

/// <summary>
/// Servi�o de dom�nio para ClientContact
/// </summary>
public class ClientContactPersonDomainService : IClientContactPersonDomainService
{
    private readonly IClientContactPersonDataRepository _repo;
    private readonly IEntityDomainValidator<ClientContactPersonsEntity> _validator;
    private readonly INotify _notify;

    public ClientContactPersonDomainService(
        IClientContactPersonDataRepository repo,
        IEntityDomainValidator<ClientContactPersonsEntity> validator,
        INotify notify)
    {
        _repo = repo;
        _validator = validator;
        _notify = notify;
    }

    public async Task<ClientContactPersonsEntity?> GetByIdAsync(int clientId, int id, CancellationToken ct)
    {
        return await _repo.GetByIdAsync(clientId, id, ct);
    }
    public async Task<IEnumerable<ClientContactPersonsEntity>> GetAllAsync(int clientId, CancellationToken ct)
    {
        return await _repo.GetAllAsync(clientId, ct);
    }
    public async Task<ListPage<ClientContactPersonsEntity>> GetPagedAsync(int clientId, PagedFilter request, CancellationToken ct)
    {
        return await _repo.GetPagedAsync(clientId, request, ct);
    }
    public async Task<bool> ExistsByClientAndEmailAsync(int clientId, string name, string email, CancellationToken ct)
    {
        return await _repo.ExistsByClientAndEmailAsync(clientId, name, email, ct);
    }

    public async Task<bool> CreateAsync(ClientContactPersonsEntity entity, CancellationToken ct)
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
    public async Task<bool> UpdateAsync(ClientContactPersonsEntity entity, CancellationToken ct)
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
    public async Task<bool> ActivateAsync(ClientContactPersonsEntity entity, CancellationToken ct)
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
    public async Task<bool> DeactivateAsync(ClientContactPersonsEntity entity, CancellationToken ct)
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
    public async Task<bool> DeleteAsync(ClientContactPersonsEntity entity, CancellationToken ct)
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
