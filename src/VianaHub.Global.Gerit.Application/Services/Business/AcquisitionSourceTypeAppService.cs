using AutoMapper;
using Microsoft.Extensions.Logging;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.AcquisitionSourceType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.AcquisitionSourceType;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using Microsoft.AspNetCore.Http;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class AcquisitionSourceTypeAppService : IAcquisitionSourceTypeAppService
{
    private readonly IAcquisitionSourceTypeDataRepository _repo;
    private readonly IAcquisitionSourceTypeDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;
    private readonly ILogger<AcquisitionSourceTypeAppService> _logger;

    public AcquisitionSourceTypeAppService(
        IAcquisitionSourceTypeDataRepository repo,
        IAcquisitionSourceTypeDomainService domain,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        IFileValidationService fileValidation,
        ILogger<AcquisitionSourceTypeAppService> logger)
    {
        _repo = repo;
        _domain = domain;
        _mapper = mapper;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _fileValidation = fileValidation;
        _logger = logger;
    }

    public async Task<IEnumerable<AcquisitionSourceTypeResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<AcquisitionSourceTypeResponse>>(entities);
    }

    public async Task<AcquisitionSourceTypeDetailResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.AcquisitionSourceType.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<AcquisitionSourceTypeDetailResponse>(entity);
    }

    public async Task<ListPageResponse<AcquisitionSourceTypeResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<AcquisitionSourceTypeResponse>>(paged);
    }

    public async Task<int> CreateAsync(CreateAcquisitionSourceTypeRequest request, CancellationToken ct)
    {
        var nameExists = await _repo.ExistsByNameAsync(request.Name, ct);
        if (nameExists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.AcquisitionSourceType.Create.NameAlreadyExists"), 409);
            return 0;
        }

        var entity = new AcquisitionSourceTypeEntity(request.Code, _currentUser.GetUserId());
        // Persistir Name e Description na tabela de traduções (idioma padrão: pt-PT)
        var translation = new AcquisitionSourceTypeTranslationsEntity(0, "pt-PT", request.Name, request.Description);
        entity.AddTranslation(translation);

        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int id, UpdateAcquisitionSourceTypeRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.AcquisitionSourceType.Update.ResourceNotFound"), 410);
            return false;
        }

        // Atualizar a tradução no idioma padrão (pt-PT) em vez de propriedades-fantasma
        var translation = entity.Translations.FirstOrDefault(t => t.LanguageCode == "pt-PT");
        if (translation != null)
        {
            translation.Update(request.Name, request.Description);
        }
        else
        {
            // Se não existir tradução pt-PT, cria uma nova
            entity.AddTranslation(new AcquisitionSourceTypeTranslationsEntity(entity.Id, "pt-PT", request.Name, request.Description));
        }

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.AcquisitionSourceType.Activate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.AcquisitionSourceType.Deactivate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.AcquisitionSourceType.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<bool> BulkUploadAsync(IFormFile file, CancellationToken ct)
    {
        _notify.Add(_localization.GetMessage("Application.Service.AcquisitionSourceType.BulkUpload.NotImplemented"), 501);
        return false;
    }
}
