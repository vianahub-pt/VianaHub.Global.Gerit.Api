using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Services.Business;

/// <summary>
/// Servi�o de dom�nio para ClientAddress
/// </summary>
public class ClientAddressDomainService : IClientAddressDomainService
{
    private readonly IClientAddressDataRepository _repo;
    private readonly IEntityDomainValidator<ClientAddressesEntity> _validator;
    private readonly INotify _notify;

    public ClientAddressDomainService(
        IClientAddressDataRepository repo,
        IEntityDomainValidator<ClientAddressesEntity> validator,
        INotify notify)
    {
        _repo = repo;
        _validator = validator;
        _notify = notify;
    }

    public async Task<ClientAddressesEntity?> GetByIdAsync(int clientId, int id, CancellationToken ct)
    {
        return await _repo.GetByIdAsync(clientId, id, ct);
    }
    public async Task<IEnumerable<ClientAddressesEntity>> GetAllAsync(int clientId, CancellationToken ct)
    {
        return await _repo.GetAllAsync(clientId, ct);
    }
    public async Task<ListPage<ClientAddressesEntity>> GetPagedAsync(int clientId, PagedFilter request, CancellationToken ct)
    {
        return await _repo.GetPagedAsync(clientId, request, ct);
    }
    public async Task<bool> ExistsByIdAsync(int id, CancellationToken ct)
    {
        return await _repo.ExistsByIdAsync(id, ct);
    }
    public async Task<bool> ExistsByClientIdAsync(int clientId, string countryCode, string street, string streetNumber, string neighborhood, string city, string district, string postalCode, CancellationToken ct)
    {
        return await _repo.ExistsByClientIdAsync(clientId, countryCode, street, streetNumber, neighborhood, city, district, postalCode, ct);
    }
    public async Task<bool> ExistsByClientAndAddressTypeAsync(int clientId, int addressTypeId, CancellationToken ct)
    {
        return await _repo.ExistsByClientAndAddressTypeAsync(clientId, addressTypeId, ct);
    }
    public async Task<bool> CreateAsync(ClientAddressesEntity entity, CancellationToken ct)
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

        return await _repo.CreateAsync(entity, ct);
    }
    public async Task<bool> UpdateAsync(ClientAddressesEntity entity, CancellationToken ct)
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
    public async Task<bool> ActivateAsync(ClientAddressesEntity entity, CancellationToken ct)
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
    public async Task<bool> DeactivateAsync(ClientAddressesEntity entity, CancellationToken ct)
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
    public async Task<bool> DeleteAsync(ClientAddressesEntity entity, CancellationToken ct)
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
