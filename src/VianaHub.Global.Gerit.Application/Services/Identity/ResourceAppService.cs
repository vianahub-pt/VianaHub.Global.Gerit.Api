using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Resource;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Resource;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Application.Services.Identity;

public class ResourceAppService : IResourceAppService
{
    private readonly IResourceDataRepository _repo;
    private readonly IResourceDomainService _domain;
    private readonly ICurrentUserService _currentUser;
    private readonly INotify _notify;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localization;
    private readonly IFileValidationService _fileValidation;

    public ResourceAppService(
        IResourceDataRepository repo,
        IResourceDomainService domain,
        INotify notify,
        IMapper mapper,
        ICurrentUserService currentUser,
        ILocalizationService localization,
        IFileValidationService fileValidation)
    {
        _repo = repo;
        _domain = domain;
        _notify = notify;
        _mapper = mapper;
        _currentUser = currentUser;
        _localization = localization;
        _fileValidation = fileValidation;
    }

    public async Task<IEnumerable<ResourceResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<ResourceResponse>>(entities);
    }
    public async Task<ResourceDetailResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Resource.Update.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<ResourceDetailResponse>(entity);
    }
    public async Task<ListPageResponse<ResourceResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<ResourceResponse>>(paged);
    }


    public async Task<int> CreateAsync(CreateResourceRequest request, CancellationToken ct)
    {
        var exists = await _repo.ExistsByNameAsync(request.Name, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Resource.Create.ResourceAlreadyExists"), 409);
            return 0;
        }

        var entity = new ResourceEntity(request.Name, request.Description, _currentUser.GetUserId());
        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }
    public async Task<bool> UpdateAsync(int id, UpdateResourceRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Resource.Update.ResourceNotFound"), 410);
            return false;
        }

        entity.Update(request.Description, _currentUser.GetUserId());
        return await _domain.UpdateAsync(entity, ct);
    }
    public async Task<bool> ActivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Resource.Activate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.Resource.Deactivate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.Resource.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }
    public async Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct)
    {
        // Valida arquivo usando servi�o centralizado
        if (!_fileValidation.ValidateFile(file))
            return false;

        // L� itens do CSV
        var items = ReadCsvFile(file);
        if (items == null)
            return false;

        if (!items.Any())
        {
            _notify.Add(_localization.GetMessage("Application.Service.Resource.BulkUpload.EmptyFile"), 400);
            return false;
        }

        // Processa cada item
        return await ProcessBulkItemsAsync(items, ct);
    }


    private List<BulkUploadResourceItem>? ReadCsvFile(IFormFile file)
    {
        try
        {
            // Cria StreamReader com encoding UTF-8 for�ado
            using var reader = file.OpenReadStream().CreateUtf8StreamReader();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ";", // CSV usa ponto e v�rgula como delimitador
                MissingFieldFound = null,
                HeaderValidated = null,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null // Ignora linhas mal formatadas ao inv�s de lan�ar exce��o
            };

            using var csv = new CsvReader(reader, config);
            var records = new List<BulkUploadResourceItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadResourceItem>();
                    if (record != null)
                    {
                        // Sanitiza e normaliza campos
                        record.Name = record.Name?.SanitizeCsvInput().NormalizeUtf8();
                        record.Description = record.Description?.SanitizeCsvInput().NormalizeUtf8();

                        // Valida se os campos n�o cont�m conte�do perigoso
                        if (!string.IsNullOrEmpty(record.Name) && !record.Name.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Resource.ReadCsvFile.Name.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.Description) && !record.Description.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Resource.ReadCsvFile.Description.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException)
                {
                    // Log linha com erro mas continua processamento
                    _notify.Add(_localization.GetMessage("Application.Service.Resource.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Resource.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Resource.ReadCsvFile.Exception"), 400);
            return null;
        }
    }
    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadResourceItem> items, CancellationToken ct)
    {
        var hasErrors = false;

        foreach (var item in items)
        {
            // Valida campos obrigat�rios
            if (!ValidateBulkItem(item))
            {
                hasErrors = true;
                continue;
            }

            // Verifica duplicidade
            var exists = await _repo.ExistsByNameAsync(item.Name, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Resource.ProcessBulkItems.ExistsByName", item.Name), 409);
                hasErrors = true;
                continue;
            }

            // Cria a entidade
            var entity = new ResourceEntity(item.Name, item.Description, _currentUser.GetUserId());

            // Tenta criar no dom�nio
            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Resource.ProcessBulkItems.FailedToCreate", item.Name), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }
    private bool ValidateBulkItem(BulkUploadResourceItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Resource.ValidateBulkItem.Name", item.Name), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.Description))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Resource.ValidateBulkItem.Description", item.Name), 400);
            return false;
        }

        return true;
    }
}
