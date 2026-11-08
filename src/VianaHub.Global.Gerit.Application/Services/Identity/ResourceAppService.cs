using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.IO;
using System.Text;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Resource;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Resource;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Mappings.Identity;
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
    private readonly ILocalizationService _localization;
    private readonly IFileValidationService _fileValidation;

    public ResourceAppService(
        IResourceDataRepository repo,
        IResourceDomainService domain,
        INotify notify,
        ICurrentUserService currentUser,
        ILocalizationService localization,
        IFileValidationService fileValidation)
    {
        _repo = repo;
        _domain = domain;
        _notify = notify;
        _currentUser = currentUser;
        _localization = localization;
        _fileValidation = fileValidation;
    }

    public async Task<IEnumerable<ResourceResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return ResourceMapper.ToResponses(entities);
    }
    public async Task<ResourceDetailResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Resource.GetById.ResourceNotFound"), 410);
            return null;
        }
        return ResourceMapper.ToDetail(entity);
    }
    public async Task<ListPageResponse<ResourceResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return ResourceMapper.ToPaged(paged);
    }


    public async Task<int> CreateAsync(CreateResourceRequest request, CancellationToken ct)
    {
        var normalizedName = request.Name?.Trim() ?? string.Empty;
        var exists = await _repo.ExistsByNameAsync(normalizedName, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Resource.Create.ResourceAlreadyExists"), 409);
            return 0;
        }

        var entity = new ResourceEntity(normalizedName, request.Description, _currentUser.GetUserId());
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

        entity.Update(request.Description?.Trim(), _currentUser.GetUserId());
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
                MissingFieldFound = _ => throw new InvalidDataException("CSV field is missing."),
                HeaderValidated = args =>
                {
                    if (args.InvalidHeaders is { Length: > 0 })
                        throw new InvalidDataException("CSV header is invalid.");
                },
                TrimOptions = TrimOptions.Trim,
                BadDataFound = _ => throw new InvalidDataException("CSV row is malformed.")
            };

            using var csv = new CsvReader(reader, config);
            var records = new List<BulkUploadResourceItem>();

            if (!csv.Read())
                throw new InvalidDataException("CSV header is missing.");
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read())
            {
                rowCount++;
                if (rowCount > maxRows)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.Resource.ReadCsvFile.MaxRows", maxRows), 400);
                    return null;
                }

                var record = csv.GetRecord<BulkUploadResourceItem>();
                if (record is null) continue;
                record.Name = record.Name?.SanitizeCsvInput().NormalizeUtf8().Trim();
                record.Description = record.Description?.SanitizeCsvInput().NormalizeUtf8().Trim();
                if (!string.IsNullOrEmpty(record.Name) && !record.Name.IsSafeCsvValue())
                    throw new InvalidDataException("CSV value is unsafe.");
                if (!string.IsNullOrEmpty(record.Description) && !record.Description.IsSafeCsvValue())
                    throw new InvalidDataException("CSV value is unsafe.");
                records.Add(record);
            }

            return records;
        }
        catch (CsvHelperException)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Resource.ReadCsvFile.Exception"), 400);
            return null;
        }
        catch (InvalidDataException)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Resource.ReadCsvFile.Exception"), 400);
            return null;
        }
        catch (IOException)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Resource.ReadCsvFile.Exception"), 400);
            return null;
        }
        catch (DecoderFallbackException)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Resource.ReadCsvFile.Exception"), 400);
            return null;
        }
        catch (InvalidOperationException)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Resource.ReadCsvFile.Exception"), 400);
            return null;
        }
    }
    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadResourceItem> items, CancellationToken ct)
    {
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entities = new List<ResourceEntity>(items.Count);

        foreach (var item in items)
        {
            // Valida campos obrigat�rios
            if (!ValidateBulkItem(item))
            {
                return false;
            }

            item.Name = item.Name!.Trim();
            item.Description = item.Description!.Trim();
            if (!seenNames.Add(item.Name))
            {
                _notify.Add(_localization.GetMessage("Application.Service.Resource.Create.ResourceAlreadyExists"), 409);
                return false;
            }

            // Verifica duplicidade
            var exists = await _repo.ExistsByNameAsync(item.Name, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Resource.ProcessBulkItems.ExistsByName", item.Name), 409);
                return false;
            }

            entities.Add(new ResourceEntity(item.Name, item.Description, _currentUser.GetUserId()));
        }

        return await _domain.CreateManyAsync(entities, ct);
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
