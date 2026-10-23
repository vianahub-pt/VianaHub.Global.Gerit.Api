using AutoMapper;
using Microsoft.Extensions.Logging;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.PartyType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.PartyType;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Business;

/// <summary>
/// Serviço de aplicação para Tipos de Party (Pessoa Física / Jurídica).
/// Orquestra operações CRUD e gere traduções como sub-recurso.
/// Catálogo global — não possui TenantId.
/// </summary>
public class PartyTypeAppService : IPartyTypeAppService
{
    private readonly IPartyTypeDataRepository _repo;
    private readonly IPartyTypeDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<PartyTypeAppService> _logger;

    public PartyTypeAppService(
        IPartyTypeDataRepository repo,
        IPartyTypeDomainService domain,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        ILogger<PartyTypeAppService> logger)
    {
        _repo = repo;
        _domain = domain;
        _mapper = mapper;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IEnumerable<PartyTypeResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<PartyTypeResponse>>(entities);
    }

    public async Task<PartyTypeDetailResponse> GetByIdAsync(byte id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.PartyType.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<PartyTypeDetailResponse>(entity);
    }

    public async Task<ListPageResponse<PartyTypeResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<PartyTypeResponse>>(paged);
    }

    public async Task<byte> CreateAsync(CreatePartyTypeRequest request, CancellationToken ct)
    {
        // Verificar duplicidade: código já existe?
        var existsByCode = await _repo.ExistsByCodeAsync(request.Code, ct);
        if (existsByCode)
        {
            _notify.Add(_localization.GetMessage("Application.Service.PartyType.Create.CodeAlreadyExists"), 409);
            return 0;
        }

        var entity = new PartyTypeEntity(
            request.Code,
            _currentUser.GetUserId());

        // Persistir Name e Description na tabela de traduções (idioma padrão: pt-PT)
        var translation = new PartyTypeTranslationsEntity(
            entity.Id, // 0 inicial, EF Core resolverá o FK após persistência
            "pt-PT",
            request.Name,
            request.Description);
        entity.Translations.Add(translation);

        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : (byte)0;
    }

    public async Task<bool> UpdateAsync(byte id, UpdatePartyTypeRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.PartyType.Update.ResourceNotFound"), 410);
            return false;
        }

        // Verificar duplicidade: se mudou o código, verificar se já existe
        if (!string.Equals(entity.Code, request.Code, StringComparison.OrdinalIgnoreCase))
        {
            var existsByCode = await _repo.ExistsByCodeAsync(request.Code, ct);
            if (existsByCode)
            {
                _notify.Add(_localization.GetMessage("Application.Service.PartyType.Update.CodeAlreadyExists"), 409);
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
            entity.Translations.Add(new PartyTypeTranslationsEntity(entity.Id, "pt-PT", request.Name, request.Description));
        }

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(byte id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.PartyType.Activate.ResourceNotFound"), 410);
            return false;
        }

        entity.Activate(_currentUser.GetUserId());
        return await _domain.ActivateAsync(entity, ct);
    }

    public async Task<bool> DeactivateAsync(byte id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.PartyType.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        entity.Deactivate(_currentUser.GetUserId());
        return await _domain.DeactivateAsync(entity, ct);
    }

    public async Task<bool> DeleteAsync(byte id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.PartyType.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<int> CreateTranslationAsync(byte id, CreatePartyTypeTranslationRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.PartyType.Translation.ResourceNotFound"), 410);
            return 0;
        }

        var translation = new PartyTypeTranslationsEntity(
            entity.Id,
            request.LanguageCode,
            request.Name,
            request.Description);

        entity.Translations.Add(translation);
        await _repo.UpdateAsync(entity, ct);
        return translation.Id;
    }

    public async Task<bool> UpdateTranslationAsync(byte id, int translationId, UpdatePartyTypeTranslationRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.PartyType.Translation.ResourceNotFound"), 410);
            return false;
        }

        var translation = entity.Translations.FirstOrDefault(t => t.Id == translationId);
        if (translation == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.PartyType.Translation.TranslationNotFound"), 410);
            return false;
        }

        translation.Update(request.Name, request.Description);
        return await _repo.UpdateAsync(entity, ct);
    }

    public async Task<bool> DeleteTranslationAsync(byte id, int translationId, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.PartyType.Translation.ResourceNotFound"), 410);
            return false;
        }

        var translation = entity.Translations.FirstOrDefault(t => t.Id == translationId);
        if (translation == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.PartyType.Translation.TranslationNotFound"), 410);
            return false;
        }

        entity.Translations.Remove(translation);
        return await _repo.UpdateAsync(entity, ct);
    }
}
