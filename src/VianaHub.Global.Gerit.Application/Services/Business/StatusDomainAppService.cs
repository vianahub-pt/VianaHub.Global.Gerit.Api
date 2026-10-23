using AutoMapper;
using Microsoft.Extensions.Logging;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDomain;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.StatusDomain;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Business;

/// <summary>
/// Serviço de aplicação para Domínios de Status.
/// Orquestra operações CRUD e gere traduções como sub-recurso.
/// Catálogo global — não possui TenantId.
/// </summary>
public class StatusDomainAppService : IStatusDomainAppService
{
    private readonly IStatusDomainDataRepository _repo;
    private readonly IStatusDomainDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<StatusDomainAppService> _logger;

    public StatusDomainAppService(
        IStatusDomainDataRepository repo,
        IStatusDomainDomainService domain,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        ILogger<StatusDomainAppService> logger)
    {
        _repo = repo;
        _domain = domain;
        _mapper = mapper;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IEnumerable<StatusDomainResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<StatusDomainResponse>>(entities);
    }

    public async Task<StatusDomainDetailResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDomain.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<StatusDomainDetailResponse>(entity);
    }

    public async Task<ListPageResponse<StatusDomainResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<StatusDomainResponse>>(paged);
    }

    public async Task<int> CreateAsync(CreateStatusDomainRequest request, CancellationToken ct)
    {
        // Verificar duplicidade: código já existe?
        var existsByCode = await _repo.ExistsByCodeAsync(request.Code, ct);
        if (existsByCode)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDomain.Create.CodeAlreadyExists"), 409);
            return 0;
        }

        var entity = new StatusDomainEntity(
            request.Code,
            _currentUser.GetUserId());

        // Persistir Name e Description na tabela de traduções (idioma padrão: pt-PT)
        var translation = new StatusDomainTranslationsEntity(
            entity.Id, // 0 inicial, EF Core resolverá o FK após persistência
            "pt-PT",
            request.Name,
            request.Description);
        entity.Translations.Add(translation);

        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int id, UpdateStatusDomainRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDomain.Update.ResourceNotFound"), 410);
            return false;
        }

        // Verificar duplicidade: se mudou o código, verificar se já existe
        if (!string.Equals(entity.Code, request.Code, StringComparison.OrdinalIgnoreCase))
        {
            var existsByCode = await _repo.ExistsByCodeAsync(request.Code, ct);
            if (existsByCode)
            {
                _notify.Add(_localization.GetMessage("Application.Service.StatusDomain.Update.CodeAlreadyExists"), 409);
                return false;
            }
        }

        entity.UpdateCode(request.Code, _currentUser.GetUserId());

        // Atualizar a tradução no idioma padrão (pt-PT)
        var translation = entity.Translations.FirstOrDefault(t => t.LanguageCode == "pt-PT");
        if (translation != null)
        {
            translation.Update(request.Name, request.Description);
        }
        else
        {
            // Se não existir tradução pt-PT, cria uma nova
            entity.Translations.Add(new StatusDomainTranslationsEntity(entity.Id, "pt-PT", request.Name, request.Description));
        }

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDomain.Activate.ResourceNotFound"), 410);
            return false;
        }

        entity.Activate(_currentUser.GetUserId());
        return await _domain.ActivateAsync(entity, ct);
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDomain.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        entity.Deactivate(_currentUser.GetUserId());
        return await _domain.DeactivateAsync(entity, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDomain.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<int> CreateTranslationAsync(int id, CreateStatusDomainTranslationRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDomain.Translation.ResourceNotFound"), 410);
            return 0;
        }

        var translation = new StatusDomainTranslationsEntity(
            entity.Id,
            request.LanguageCode,
            request.Name,
            request.Description);

        entity.Translations.Add(translation);
        await _repo.UpdateAsync(entity, ct);
        return translation.Id;
    }

    public async Task<bool> UpdateTranslationAsync(int id, int translationId, UpdateStatusDomainTranslationRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDomain.Translation.ResourceNotFound"), 410);
            return false;
        }

        var translation = entity.Translations.FirstOrDefault(t => t.Id == translationId);
        if (translation == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDomain.Translation.TranslationNotFound"), 410);
            return false;
        }

        translation.Update(request.Name, request.Description);
        return await _repo.UpdateAsync(entity, ct);
    }

    public async Task<bool> DeleteTranslationAsync(int id, int translationId, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDomain.Translation.ResourceNotFound"), 410);
            return false;
        }

        var translation = entity.Translations.FirstOrDefault(t => t.Id == translationId);
        if (translation == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDomain.Translation.TranslationNotFound"), 410);
            return false;
        }

        entity.Translations.Remove(translation);
        return await _repo.UpdateAsync(entity, ct);
    }
}
