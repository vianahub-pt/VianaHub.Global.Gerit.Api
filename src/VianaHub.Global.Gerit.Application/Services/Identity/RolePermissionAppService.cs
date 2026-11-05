using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Role;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.RolePermission;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.RolePermission;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
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
    private readonly IMapper _mapper;
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
        IMapper mapper,
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
        _mapper = mapper;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _fileValidation = fileValidation;
    }

    public async Task<RolePermissionResponse?> CreateAsync(CreateRolePermissionRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();

        // Valida duplicidade
        var exists = await _repository.ExistsAsync(tenantId, request.RoleId, request.ResourceId, request.ActionId, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.Create.ResourceAlreadyExists"), 400);
            return null;
        }

        // Valida exist�ncia de role/resource/action
        var roleExists = await _roleRepository.ExistsByIdAsync(request.RoleId, ct);
        if (!roleExists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.Create.RoleNotFound"), 404);
            return null;
        }

        var resourceExists = await _resourceRepository.ExistsByIdAsync(request.ResourceId, ct);
        if (!resourceExists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.Create.ResourceNotFound"), 404);
            return null;
        }

        var actionExists = await _actionRepository.ExistsByIdAsync(request.ActionId, ct);
        if (!actionExists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.Create.ActionNotFound"), 404);
            return null;
        }

        var entity = new RolePermissionsEntity(tenantId, request.RoleId, request.ResourceId, request.ActionId);
        await _domain.CreateAsync(entity, ct);
        return _mapper.Map<RolePermissionResponse>(entity);
    }

    public async Task DeleteAsync(int roleId, int resourceId, int actionId, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var entity = await _repository.GetByIdAsync(tenantId, roleId, resourceId, actionId, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.Delete.ResourceNotFound"), 410);
            return;
        }

        await _repository.DeleteAsync(tenantId, roleId, resourceId, actionId, ct);
    }

    public async Task<RolePermissionDetailResponse> GetByIdAsync(int roleId, int resourceId, int actionId, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var entity = await _repository.GetByIdAsync(tenantId, roleId, resourceId, actionId, ct);
        return _mapper.Map<RolePermissionDetailResponse>(entity);
    }

    public async Task<IList<RolePermissionResponse>> GetByRoleAsync(int roleId, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var list = await _repository.GetByRoleAsync(roleId, tenantId, ct);
        return _mapper.Map<IList<RolePermissionResponse>>(list);
    }

    public async Task<IList<RolePermissionResponse>> GetByResourceAsync(int resourceId, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var list = await _repository.GetByResourceAsync(resourceId, tenantId, ct);
        return _mapper.Map<IList<RolePermissionResponse>>(list);
    }

    public async Task<IList<RolePermissionResponse>> GetAllAsync(CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var list = await _repository.GetAllAsync(tenantId, ct);
        return _mapper.Map<IList<RolePermissionResponse>>(list);
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
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.BulkUpload.EmptyFile"), 400);
            return false;
        }

        // Processa cada item
        return await ProcessBulkItemsAsync(items, ct);
    }

    private List<BulkUploadRolePermissionItem>? ReadCsvFile(IFormFile file)
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
            var records = new List<BulkUploadRolePermissionItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadRolePermissionItem>();
                    if (record != null)
                    {
                        // Valida se os campos n�o cont�m conte�do perigoso
                        if (record.RoleId <= 0)
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ReadCsvFile.RoleId.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }
                        if (record.ResourceId <= 0)
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ReadCsvFile.ResourceId.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }
                        if (record.ActionId <= 0)
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ReadCsvFile.ActionId.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException)
                {
                    // Log linha com erro mas continua processamento
                    _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadRolePermissionItem> items, CancellationToken ct)
    {
        var hasErrors = false;
        var tenantId = _currentUser.GetTenantId();

        foreach (var item in items)
        {
            // Valida campos obrigat�rios
            if (!ValidateBulkItem(item))
            {
                hasErrors = true;
                continue;
            }

            // Verifica duplicidade
            var exists = await _repository.ExistsAsync(tenantId, item.RoleId, item.ResourceId, item.ActionId, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ProcessBulkItems.ExistsByName"), 400);
                hasErrors = true;
                continue;
            }

            // Cria a entidade
            var entity = new RolePermissionsEntity(tenantId, item.RoleId, item.ResourceId, item.ActionId);

            // Tenta criar no dom�nio
            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ProcessBulkItems.FailedToCreate"), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadRolePermissionItem item)
    {
        if (item.RoleId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ValidateBulkItem.RoleId", item.RoleId), 400);
            return false;
        }

        if (item.ResourceId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ValidateBulkItem.ResourceId", item.ResourceId), 400);
            return false;
        }

        if (item.ActionId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.RolePermission.ValidateBulkItem.ActionId", item.ActionId), 400);
            return false;
        }

        return true;
    }
}
