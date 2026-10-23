using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.User;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.UserRole;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.UserRole;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Identity;

public class UserRoleAppService : IUserRoleAppService
{
    private readonly IUserRoleDomainService _domain;
    private readonly IUserRoleDataRepository _repository;
    private readonly IRoleDataRepository _roleRepository;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;

    public UserRoleAppService(
        IUserRoleDomainService domain,
        IUserRoleDataRepository repository,
        IRoleDataRepository roleRepository,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        IFileValidationService fileValidation)
    {
        _domain = domain;
        _repository = repository;
        _roleRepository = roleRepository;
        _mapper = mapper;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _fileValidation = fileValidation;
    }

    public async Task<UserRoleDetailResponse> GetByIdAsync(int userId, int roleId, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var entity = await _repository.GetByIdAsync(tenantId, userId, roleId, ct);
        return _mapper.Map<UserRoleDetailResponse>(entity);
    }

    public async Task<IList<UserRoleResponse>> GetByUserAsync(int userId, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var list = await _repository.GetByUserAsync(tenantId, userId, ct);
        return _mapper.Map<IList<UserRoleResponse>>(list);
    }

    public async Task<IList<UserRoleResponse>> GetByRoleAsync(int roleId, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var list = await _repository.GetByRoleAsync(tenantId, roleId, ct);
        return _mapper.Map<IList<UserRoleResponse>>(list);
    }

    public async Task<IList<UserRoleResponse>> GetAllAsync(CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var list = await _repository.GetAllAsync(tenantId, ct);
        return _mapper.Map<IList<UserRoleResponse>>(list);
    }
    public async Task<bool> ExistsAsync(int tenantId, int userId, int roleId, CancellationToken ct)
    {
        return await _repository.ExistsAsync(tenantId, userId, roleId, ct);
    }
    public async Task<UserRoleResponse> CreateAsync(CreateUserRoleRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();

        // Verifica duplicidade
        var existing = await _repository.GetByUserAsync(tenantId, request.UserId, ct);
        if (existing != null && existing.Any(x => x.RoleId == request.RoleId))
        {
            _notify.Add(_localization.GetMessage("Application.Service.UserRole.Create.ResourceAlreadyExists"), 400);
            return null;
        }

        // Valida se o Role existe
        var roleExists = await _roleRepository.ExistsByIdAsync(request.RoleId, CancellationToken.None);
        if (!roleExists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.UserRole.Create.RoleNotFound"), 404);
            return null;
        }

        var entity = new UserRolesEntity(tenantId, request.UserId, request.RoleId);
        await _domain.CreateAsync(entity, ct);
        return _mapper.Map<UserRoleResponse>(entity);
    }


    public async Task DeleteAsync(int userId, int roleId, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var entity = await _repository.GetByIdAsync(tenantId, userId, roleId, ct);
        if (entity == null)
        {
            // Not found -> notify with 410 Gone
            _notify.Add(_localization.GetMessage("Application.Service.UserRole.Delete.ResourceNotFound"), 410);
            return;
        }

        await _repository.DeleteAsync(tenantId, userId, roleId, ct);
    }


    public async Task<bool> BulkUploadAsync(IFormFile file, CancellationToken ct)
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
            _notify.Add(_localization.GetMessage("Application.Service.UserRole.BulkUpload.EmptyFile"), 400);
            return false;
        }

        // Processa cada item
        return await ProcessBulkItemsAsync(items, ct);
    }

    private List<BulkUploadUserRoleItem> ReadCsvFile(IFormFile file)
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
            var records = new List<BulkUploadUserRoleItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadUserRoleItem>();
                    if (record != null)
                    {
                        // Valida se os campos n�o cont�m conte�do perigoso
                        if (record.UserId <= 0)
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.UserRole.ReadCsvFile.UserId.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (record.RoleId <= 0)
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.UserRole.ReadCsvFile.RoleId.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException ex)
                {
                    // Log linha com erro mas continua processamento
                    _notify.Add(_localization.GetMessage("Application.Service.UserRole.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.UserRole.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception ex)
        {
            _notify.Add(_localization.GetMessage("Application.Service.UserRole.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadUserRoleItem> items, CancellationToken ct)
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
            var exists = await _repository.ExistsAsync(tenantId, item.UserId, item.RoleId, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.UserRole.ProcessBulkItems.ExistsByEmail"), 400);
                hasErrors = true;
                continue;
            }

            var entity = new UserRolesEntity(tenantId, item.UserId, item.RoleId);

            // Tenta criar no dom�nio
            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.UserRole.ProcessBulkItems.FailedToCreate"), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadUserRoleItem item)
    {
        if (item.UserId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.UserRole.ValidateBulkItem.UserId", item.UserId), 400);
            return false;
        }
        if (item.RoleId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.UserRole.ValidateBulkItem.RoleId", item.RoleId), 400);
            return false;
        }

        return true;
    }
}
