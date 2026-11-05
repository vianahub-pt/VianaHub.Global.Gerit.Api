using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientDocument;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientDocument;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Mappings.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class ClientDocumentAppService : IClientDocumentAppService
{
    private readonly IClientDocumentDataRepository _repo;
    private readonly IClientRepository _clients;
    private readonly IDocumentTypeDataRepository _documentTypes;
    private readonly IClientDocumentDomainService _domain;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;

    public ClientDocumentAppService(IClientDocumentDataRepository repo, IClientRepository clients, IDocumentTypeDataRepository documentTypes,
        IClientDocumentDomainService domain, INotify notify, ILocalizationService localization, ICurrentUserService currentUser)
    {
        _repo = repo; _clients = clients; _documentTypes = documentTypes; _domain = domain; _notify = notify; _localization = localization; _currentUser = currentUser;
    }

    public async Task<IEnumerable<ClientDocumentResponse>> GetAllAsync(int clientId, CancellationToken ct) =>
        (await _repo.GetAllAsync(_currentUser.GetTenantId(), clientId, ct)).Select(ClientDocumentMapper.ToResponse);

    public async Task<ClientDocumentDetailResponse?> GetByIdAsync(int clientId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(_currentUser.GetTenantId(), clientId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.ResourceNotFound"), 410);
            return null;
        }
        return ClientDocumentMapper.ToDetail(entity);
    }

    public async Task<ListPageResponse<ClientDocumentResponse>> GetPagedAsync(int clientId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        return ClientDocumentMapper.ToPaged(await _repo.GetPagedAsync(_currentUser.GetTenantId(), clientId, filter, ct));
    }

    public async Task<int> CreateAsync(int clientId, CreateClientDocumentRequest request, CancellationToken ct)
    {
        if (!await EnsureActiveClientAsync(clientId, "Create", ct) || !await EnsureActiveDocumentTypeAsync(request.DocumentTypeId, "Create", ct)) return 0;
        var tenantId = _currentUser.GetTenantId();
        var country = request.IssuingCountryCode.Trim().ToUpperInvariant();
        var number = request.DocumentNumber.Trim();
        if (await _repo.ExistsDuplicateAsync(tenantId, request.DocumentTypeId, country, number, null, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.DuplicateDocument"), 409);
            return 0;
        }
        var entity = new ClientDocumentsEntity(tenantId, clientId, request.DocumentTypeId, number, country, request.IssuedAt, request.ExpiresAt, request.IsPrimary, _currentUser.GetUserId());
        return await _domain.CreateAsync(entity, ct) ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int clientId, int id, UpdateClientDocumentRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(_currentUser.GetTenantId(), clientId, id, ct);
        if (entity == null || entity.IsDeleted) return NotFound("Update");
        if (!await EnsureActiveClientAsync(clientId, "Update", ct) || !await EnsureActiveDocumentTypeAsync(request.DocumentTypeId, "Update", ct)) return false;
        var country = request.IssuingCountryCode.Trim().ToUpperInvariant();
        var number = request.DocumentNumber.Trim();
        if (await _repo.ExistsDuplicateAsync(entity.TenantId, request.DocumentTypeId, country, number, id, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.DuplicateDocument"), 409);
            return false;
        }
        entity.Update(number, country, request.IssuedAt, request.ExpiresAt, request.IsPrimary, _currentUser.GetUserId());
        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int clientId, int id, CancellationToken ct) => await ChangeStateAsync(clientId, id, true, ct);
    public async Task<bool> DeactivateAsync(int clientId, int id, CancellationToken ct) => await ChangeStateAsync(clientId, id, false, ct);

    public async Task<bool> DeleteAsync(int clientId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(_currentUser.GetTenantId(), clientId, id, ct);
        if (entity == null || entity.IsDeleted) return NotFound("Delete");
        if (!await EnsureActiveClientAsync(clientId, "Delete", ct)) return false;
        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    private async Task<bool> ChangeStateAsync(int clientId, int id, bool activate, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(_currentUser.GetTenantId(), clientId, id, ct);
        if (entity == null || entity.IsDeleted) return NotFound(activate ? "Activate" : "Deactivate");
        if (!await EnsureActiveClientAsync(clientId, activate ? "Activate" : "Deactivate", ct)) return false;
        if (activate) entity.Activate(_currentUser.GetUserId()); else entity.Deactivate(_currentUser.GetUserId());
        return activate ? await _domain.ActivateAsync(entity, ct) : await _domain.DeactivateAsync(entity, ct);
    }

    private async Task<bool> EnsureActiveClientAsync(int clientId, string operation, CancellationToken ct)
    {
        var client = await _clients.GetByIdAsync(clientId, ct);
        if (client != null && client.IsActive && !client.IsDeleted) return true;
        _notify.Add(_localization.GetMessage($"Application.Service.ClientDocument.{operation}.ClientNotFound"), 410);
        return false;
    }

    private async Task<bool> EnsureActiveDocumentTypeAsync(int id, string operation, CancellationToken ct)
    {
        var type = id > 0 ? await _documentTypes.GetByIdAsync(id, ct) : null;
        if (type == null || type.IsDeleted)
        {
            _notify.Add(_localization.GetMessage($"Application.Service.ClientDocument.{operation}.DocumentTypeNotFound"), 410);
            return false;
        }
        if (!type.IsActive)
        {
            _notify.Add(_localization.GetMessage($"Application.Service.ClientDocument.{operation}.DocumentTypeInactive"), 409);
            return false;
        }
        return true;
    }

    private bool NotFound(string operation)
    {
        _notify.Add(_localization.GetMessage($"Application.Service.ClientDocument.{operation}.ResourceNotFound"), 410);
        return false;
    }
}
