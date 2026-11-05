using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Services.Business;

public class PartyTypeDomainService : IPartyTypeDomainService
{
    private readonly IPartyTypeDataRepository _repo;
    private readonly IEntityDomainValidator<PartyTypeEntity> _validator;
    private readonly INotify _notify;

    public PartyTypeDomainService(
        IPartyTypeDataRepository repo,
        IEntityDomainValidator<PartyTypeEntity> validator,
        INotify notify)
    {
        _repo = repo;
        _validator = validator;
        _notify = notify;
    }

    public async Task<PartyTypeEntity?> GetByIdAsync(byte id, CancellationToken ct)
        => await _repo.GetByIdAsync(id, ct);

    public async Task<IEnumerable<PartyTypeEntity>> GetAllAsync(CancellationToken ct)
        => await _repo.GetAllAsync(ct);

    public async Task<ListPage<PartyTypeEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
        => await _repo.GetPagedAsync(request, ct);

    public async Task<bool> ExistsByIdAsync(byte id, CancellationToken ct)
        => await _repo.ExistsByIdAsync(id, ct);

    public async Task<bool> CreateAsync(PartyTypeEntity entity, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateForCreateAsync(entity);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
                _notify.Add(error.ErrorMessage, 400);
            return false;
        }
        await _repo.AddAsync(entity, ct);
        return true;
    }

    public async Task<bool> UpdateAsync(PartyTypeEntity entity, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateForUpdateAsync(entity);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
                _notify.Add(error.ErrorMessage, 400);
            return false;
        }
        await _repo.UpdateAsync(entity, ct);
        return true;
    }

    public async Task<bool> ActivateAsync(PartyTypeEntity entity, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateForActivateAsync(entity);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
                _notify.Add(error.ErrorMessage, 400);
            return false;
        }
        await _repo.UpdateAsync(entity, ct);
        return true;
    }

    public async Task<bool> DeactivateAsync(PartyTypeEntity entity, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateForDeactivateAsync(entity);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
                _notify.Add(error.ErrorMessage, 400);
            return false;
        }
        await _repo.UpdateAsync(entity, ct);
        return true;
    }

    public async Task<bool> DeleteAsync(PartyTypeEntity entity, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateForDeleteAsync(entity);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
                _notify.Add(error.ErrorMessage, 400);
            return false;
        }
        await _repo.UpdateAsync(entity, ct);
        return true;
    }
}
