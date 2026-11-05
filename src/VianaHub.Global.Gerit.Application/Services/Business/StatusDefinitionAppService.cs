using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Constants;
using AutoMapper;
using Microsoft.Extensions.Logging;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDefinition;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.StatusDefinition;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Business;

/// <summary>
/// Serviço de aplicação para Definições de Status.
/// Orquestra operações CRUD e gere traduções como sub-recurso.
/// </summary>
public class StatusDefinitionAppService : IStatusDefinitionAppService
{
    private readonly IStatusDefinitionDataRepository _repo;
    private readonly IStatusDefinitionDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<StatusDefinitionAppService> _logger;

    public StatusDefinitionAppService(
        IStatusDefinitionDataRepository repo,
        IStatusDefinitionDomainService domain,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        ILogger<StatusDefinitionAppService> logger)
    {
        _repo = repo;
        _domain = domain;
        _mapper = mapper;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IEnumerable<StatusDefinitionResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<StatusDefinitionResponse>>(entities);
    }

    public async Task<StatusDefinitionDetailResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDefinition.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<StatusDefinitionDetailResponse>(entity);
    }

    public async Task<ListPageResponse<StatusDefinitionResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<StatusDefinitionResponse>>(paged);
    }

    public async Task<int> CreateAsync(CreateStatusDefinitionRequest request, CancellationToken ct)
    {
        var code = CatalogCodeGenerator.FromCanonicalTranslation(request.Translations, t => t.LanguageCode, t => t.Name);
        if (request.Translations is null || string.IsNullOrEmpty(code) || request.Translations.Any(t => t == null))
        {
            _notify.Add(_localization.GetMessage("Application.Catalog.CanonicalEnglishNameRequired"), 400);
            return 0;
        }
        if (request.Translations!.Select(t => t.LanguageCode).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Translations.Count)
        {
            _notify.Add(_localization.GetMessage("Application.Catalog.TranslationLanguagesUnique"), 400);
            return 0;
        }
        // Verificar duplicidade: código já existe para o mesmo domínio?
        var existsByCode = await _repo.ExistsByCodeAndDomainAsync(request.StatusDomainId, code, ct);
        if (existsByCode)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDefinition.Create.CodeAlreadyExists"), 409);
            return 0;
        }

        foreach (var translation in request.Translations!)
        {
            if (await _repo.ExistsByTranslationAndDomainAsync(request.StatusDomainId, translation.LanguageCode, translation.Name, ct))
            {
                _notify.Add(_localization.GetMessage("Application.Catalog.TranslationAlreadyExists"), 409);
                return 0;
            }
        }

        var entity = new StatusDefinitionEntity(
            _currentUser.GetTenantId(),
            request.StatusDomainId,
            code,
            request.DisplayOrder,
            request.IsSystem,
            _currentUser.GetUserId());

        // Adicionar traduções se fornecidas
        if (request.Translations != null && request.Translations.Any())
        {
            foreach (var translationDto in request.Translations.Where(t => t != null))
            {
                var translation = new StatusDefinitionTranslationsEntity(
                    entity.TenantId,
                    entity.StatusDomainId,
                    entity.Id, // 0 inicial, EF Core resolverá o FK após persistência
                    translationDto.LanguageCode,
                    translationDto.Name,
                    translationDto.Description);
                entity.Translations.Add(translation);
            }
        }

        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int id, UpdateStatusDefinitionRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDefinition.Update.ResourceNotFound"), 410);
            return false;
        }

        entity.Update(request.DisplayOrder, request.IsSystem, _currentUser.GetUserId());
        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDefinition.Activate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.StatusDefinition.Deactivate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.StatusDefinition.Delete.ResourceNotFound"), 410);
            return false;
        }

        // Proteger status de sistema contra eliminação
        if (entity.IsSystem)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDefinition.Delete.SystemStatusCannotBeDeleted"), 400);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<int> CreateTranslationAsync(int id, CreateStatusDefinitionTranslationRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDefinition.Translation.ResourceNotFound"), 410);
            return 0;
        }

        var translation = new StatusDefinitionTranslationsEntity(
            entity.TenantId,
            entity.StatusDomainId,
            entity.Id,
            request.LanguageCode,
            request.Name,
            request.Description);

        entity.Translations.Add(translation);
        await _repo.UpdateAsync(entity, ct);
        return translation.Id;
    }

    public async Task<bool> UpdateTranslationAsync(int id, int translationId, UpdateStatusDefinitionTranslationRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDefinition.Translation.ResourceNotFound"), 410);
            return false;
        }

        var translation = entity.Translations.FirstOrDefault(t => t.Id == translationId);
        if (translation == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDefinition.Translation.TranslationNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.StatusDefinition.Translation.ResourceNotFound"), 410);
            return false;
        }

        var translation = entity.Translations.FirstOrDefault(t => t.Id == translationId);
        if (translation == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.StatusDefinition.Translation.TranslationNotFound"), 410);
            return false;
        }

        entity.Translations.Remove(translation);
        return await _repo.UpdateAsync(entity, ct);
    }
}
