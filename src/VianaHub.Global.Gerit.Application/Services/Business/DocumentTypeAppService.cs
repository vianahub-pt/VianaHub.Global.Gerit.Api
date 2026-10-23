using AutoMapper;
using Microsoft.Extensions.Logging;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.DocumentType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.DocumentType;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Business;

/// <summary>
/// Serviço de aplicação para Tipos de Documento (BI, Passaporte, NIF, etc.).
/// Orquestra operações CRUD e gere traduções como sub-recurso.
/// </summary>
public class DocumentTypeAppService : IDocumentTypeAppService
{
    private readonly IDocumentTypeDataRepository _repo;
    private readonly IDocumentTypeDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DocumentTypeAppService> _logger;

    public DocumentTypeAppService(
        IDocumentTypeDataRepository repo,
        IDocumentTypeDomainService domain,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        ILogger<DocumentTypeAppService> logger)
    {
        _repo = repo;
        _domain = domain;
        _mapper = mapper;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IEnumerable<DocumentTypeResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<DocumentTypeResponse>>(entities);
    }

    public async Task<DocumentTypeDetailResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.DocumentType.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<DocumentTypeDetailResponse>(entity);
    }

    public async Task<ListPageResponse<DocumentTypeResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<DocumentTypeResponse>>(paged);
    }

    public async Task<int> CreateAsync(CreateDocumentTypeRequest request, CancellationToken ct)
    {
        // Verificar duplicidade: código já existe?
        var existsByCode = await _repo.ExistsByCodeAsync(request.Code, ct);
        if (existsByCode)
        {
            _notify.Add(_localization.GetMessage("Application.Service.DocumentType.Create.CodeAlreadyExists"), 409);
            return 0;
        }

        var entity = new DocumentTypeEntity(
            request.Code,
            _currentUser.GetUserId());

        // Adicionar traduções se fornecidas
        if (request.Translations != null && request.Translations.Any())
        {
            foreach (var translationDto in request.Translations)
            {
                var translation = new DocumentTypeTranslationsEntity(
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

    public async Task<bool> UpdateAsync(int id, UpdateDocumentTypeRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.DocumentType.Update.ResourceNotFound"), 410);
            return false;
        }

        // Verificar duplicidade: se mudou o código, verificar se já existe
        if (!string.Equals(entity.Code, request.Code, StringComparison.OrdinalIgnoreCase))
        {
            var existsByCode = await _repo.ExistsByCodeAsync(request.Code, ct);
            if (existsByCode)
            {
                _notify.Add(_localization.GetMessage("Application.Service.DocumentType.Update.CodeAlreadyExists"), 409);
                return false;
            }
        }

        entity.UpdateCode(request.Code, _currentUser.GetUserId());
        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.DocumentType.Activate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.DocumentType.Deactivate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.DocumentType.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<int> CreateTranslationAsync(int id, CreateDocumentTypeTranslationRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.DocumentType.Translation.ResourceNotFound"), 410);
            return 0;
        }

        var translation = new DocumentTypeTranslationsEntity(
            entity.Id,
            request.LanguageCode,
            request.Name,
            request.Description);

        entity.Translations.Add(translation);
        await _repo.UpdateAsync(entity, ct);
        return translation.Id;
    }

    public async Task<bool> UpdateTranslationAsync(int id, int translationId, UpdateDocumentTypeTranslationRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.DocumentType.Translation.ResourceNotFound"), 410);
            return false;
        }

        var translation = entity.Translations.FirstOrDefault(t => t.Id == translationId);
        if (translation == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.DocumentType.Translation.TranslationNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.DocumentType.Translation.ResourceNotFound"), 410);
            return false;
        }

        var translation = entity.Translations.FirstOrDefault(t => t.Id == translationId);
        if (translation == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.DocumentType.Translation.TranslationNotFound"), 410);
            return false;
        }

        entity.Translations.Remove(translation);
        return await _repo.UpdateAsync(entity, ct);
    }
}
