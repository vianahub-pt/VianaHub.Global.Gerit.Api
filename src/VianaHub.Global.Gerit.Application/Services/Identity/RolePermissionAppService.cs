using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Text;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.RolePermission;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.RolePermission;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Application.Mappings.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Identity;

public class RolePermissionAppService : IRolePermissionAppService
{
    private readonly IRolePermissionDomainService _domain;
    private readonly IRolePermissionDataRepository _repository;
    private readonly IRoleDataRepository _roleRepository;
    private readonly IResourceDataRepository _resourceRepository;
    private readonly IActionDataRepository _actionRepository;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;

    public RolePermissionAppService(
        IRolePermissionDomainService domain,
        IRolePermissionDataRepository repository,
        IRoleDataRepository roleRepository,
        IResourceDataRepository resourceRepository,
        IActionDataRepository actionRepository,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        IFileValidationService fileValidation)
    {
        _domain = domain;
        _repository = repository;
        _roleRepository = roleRepository;
        _resourceRepository = resourceRepository;
        _actionRepository = actionRepository;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _fileValidation = fileValidation;
    }

    public async Task<RolePermissionResponse?> CreateAsync(CreateRolePermissionRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        if (!ValidateIds(request.RoleId, request.ResourceId, request.ActionId))
            return null;

        var exists = await _repository.ExistsAsync(tenantId, request.RoleId, request.ResourceId, request.ActionId, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.Create.ResourceAlreadyExists"), 409);
            return null;
        }

        var role = await _roleRepository.GetByIdAsync(request.RoleId, ct);
        if (role is null || role.TenantId != tenantId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.Create.RoleNotFound"), 404);
            return null;
        }

        if (!await _resourceRepository.ExistsByIdAsync(request.ResourceId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.Create.ResourceNotFound"), 404);
            return null;
        }

        if (!await _actionRepository.ExistsByIdAsync(request.ActionId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.Create.ActionNotFound"), 404);
            return null;
        }

        var entity = new RolePermissionsEntity(tenantId, request.RoleId, request.ResourceId, request.ActionId);
        return await _domain.CreateAsync(entity, ct)
            ? RolePermissionMapper.ToResponse(entity)
            : null;
    }

    public async Task DeleteAsync(int roleId, int resourceId, int actionId, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var entity = await _repository.GetByIdAsync(tenantId, roleId, resourceId, actionId, ct);
        if (entity is null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.Delete.ResourceNotFound"), 410);
            return;
        }

        await _repository.DeleteAsync(tenantId, roleId, resourceId, actionId, ct);
    }

    public async Task<RolePermissionDetailResponse?> GetByIdAsync(int roleId, int resourceId, int actionId, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var entity = await _repository.GetByIdAsync(tenantId, roleId, resourceId, actionId, ct);
        if (entity is null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.GetById.ResourceNotFound"), 410);
            return null;
        }

        return RolePermissionMapper.ToDetail(entity);
    }

    public async Task<IList<RolePermissionResponse>> GetByRoleAsync(int roleId, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var list = await _repository.GetByRoleAsync(roleId, tenantId, ct);
        return RolePermissionMapper.ToResponses(list);
    }

    public async Task<IList<RolePermissionResponse>> GetByResourceAsync(int resourceId, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var list = await _repository.GetByResourceAsync(resourceId, tenantId, ct);
        return RolePermissionMapper.ToResponses(list);
    }

    public async Task<IList<RolePermissionResponse>> GetAllAsync(CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var list = await _repository.GetAllAsync(tenantId, ct);
        return RolePermissionMapper.ToResponses(list);
    }

    public async Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct)
    {
        if (!_fileValidation.ValidateFile(file))
            return false;

        var items = ReadCsvFile(file!);
        if (items is null)
            return false;

        if (items.Count == 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.BulkUpload.EmptyFile"), 400);
            return false;
        }

        return await ProcessBulkItemsAsync(items, ct);
    }

    private List<BulkUploadRolePermissionItem>? ReadCsvFile(IFormFile file)
    {
        try
        {
            using var reader = file.OpenReadStream().CreateUtf8StreamReader();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ";",
                TrimOptions = TrimOptions.Trim
            };

            using var csv = new CsvReader(reader, config);
            if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord is null)
            {
                _notify.Add(_localization.GetMessage("Application.Service.RolePermission.BulkUpload.EmptyFile"), 400);
                return null;
            }

            var expectedHeaders = new[] { "RoleId", "ResourceId", "ActionId" };
            if (csv.HeaderRecord.Length != expectedHeaders.Length ||
                !expectedHeaders.SequenceEqual(csv.HeaderRecord, StringComparer.OrdinalIgnoreCase))
            {
                _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ReadCsvFile.InvalidHeader"), 400);
                return null;
            }

            var records = new List<BulkUploadRolePermissionItem>();
            var rowCount = 0;
            var maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read())
            {
                if (rowCount >= maxRows)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ReadCsvFile.MaxRows", maxRows), 400);
                    return null;
                }

                var record = csv.GetRecord<BulkUploadRolePermissionItem>();
                if (record is null || !ValidateIds(record.RoleId, record.ResourceId, record.ActionId, rowCount + 2))
                    return null;

                records.Add(record);
                rowCount++;
            }

            return records;
        }
        catch (CsvHelperException)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ReadCsvFile.Exception"), 400);
            return null;
        }
        catch (InvalidDataException)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ReadCsvFile.Exception"), 400);
            return null;
        }
        catch (IOException)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ReadCsvFile.Exception"), 400);
            return null;
        }
        catch (DecoderFallbackException)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadRolePermissionItem> items, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var keys = new HashSet<(int RoleId, int ResourceId, int ActionId)>();
        var entities = new List<RolePermissionsEntity>(items.Count);

        foreach (var item in items)
        {
            if (!ValidateIds(item.RoleId, item.ResourceId, item.ActionId))
                return false;

            if (!keys.Add((item.RoleId, item.ResourceId, item.ActionId)))
            {
                _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ProcessBulkItems.Duplicate"), 409);
                return false;
            }

            if (await _repository.ExistsAsync(tenantId, item.RoleId, item.ResourceId, item.ActionId, ct))
            {
                _notify.Add(_localization.GetMessage("Application.Service.RolePermission.Create.ResourceAlreadyExists"), 409);
                return false;
            }

            var role = await _roleRepository.GetByIdAsync(item.RoleId, ct);
            if (role is null || role.TenantId != tenantId)
            {
                _notify.Add(_localization.GetMessage("Application.Service.RolePermission.Create.RoleNotFound"), 404);
                return false;
            }

            if (!await _resourceRepository.ExistsByIdAsync(item.ResourceId, ct))
            {
                _notify.Add(_localization.GetMessage("Application.Service.RolePermission.Create.ResourceNotFound"), 404);
                return false;
            }

            if (!await _actionRepository.ExistsByIdAsync(item.ActionId, ct))
            {
                _notify.Add(_localization.GetMessage("Application.Service.RolePermission.Create.ActionNotFound"), 404);
                return false;
            }

            entities.Add(new RolePermissionsEntity(tenantId, item.RoleId, item.ResourceId, item.ActionId));
        }

        return await _domain.CreateManyAsync(entities, ct);
    }

    private bool ValidateIds(int roleId, int resourceId, int actionId, int? rowNumber = null)
    {
        if (roleId <= 0)
        {
            AddIdError("RoleId", "Domain.RolePermission.RoleIdRequired", rowNumber);
            return false;
        }

        if (resourceId <= 0)
        {
            AddIdError("ResourceId", "Domain.RolePermission.ResourceIdRequired", rowNumber);
            return false;
        }

        if (actionId <= 0)
        {
            AddIdError("ActionId", "Domain.RolePermission.ActionIdRequired", rowNumber);
            return false;
        }

        return true;
    }

    private void AddIdError(string field, string key, int? rowNumber)
    {
        var message = rowNumber.HasValue
            ? _localization.GetMessage($"Application.Service.RolePermission.ValidateBulkItem.{field}", rowNumber.Value)
            : _localization.GetMessage(key);
        _notify.Add(message, 400);
    }
}
