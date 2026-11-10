using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.UserRole;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.UserRole;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Mappings.Identity;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Exceptions;
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
    private readonly IUserDataRepository _userRepository;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;

    public UserRoleAppService(
        IUserRoleDomainService domain,
        IUserRoleDataRepository repository,
        IRoleDataRepository roleRepository,
        IUserDataRepository userRepository,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        IFileValidationService fileValidation)
    {
        _domain = domain;
        _repository = repository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _fileValidation = fileValidation;
    }

    public async Task<UserRoleDetailResponse> GetByIdAsync(int userId, int roleId, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var entity = await _repository.GetByIdAsync(tenantId, userId, roleId, ct);
        if (entity is not null)
            return UserRoleMapper.ToDetail(entity);

        _notify.Add(_localization.GetMessage("Application.Service.UserRole.GetById.ResourceNotFound"), 410);
        return null!;
    }

    public async Task<IList<UserRoleResponse>> GetByUserAsync(int userId, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var list = await _repository.GetByUserAsync(tenantId, userId, ct);
        return UserRoleMapper.ToResponses(list);
    }

    public async Task<IList<UserRoleResponse>> GetByRoleAsync(int roleId, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var list = await _repository.GetByRoleAsync(tenantId, roleId, ct);
        return UserRoleMapper.ToResponses(list);
    }

    public async Task<IList<UserRoleResponse>> GetAllAsync(CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var list = await _repository.GetAllAsync(tenantId, ct);
        return UserRoleMapper.ToResponses(list);
    }
    public async Task<bool> ExistsAsync(int tenantId, int userId, int roleId, CancellationToken ct)
    {
        return await _repository.ExistsAsync(tenantId, userId, roleId, ct);
    }
    public async Task<UserRoleResponse?> CreateAsync(CreateUserRoleRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();

        if (!await ValidateParentsAsync(tenantId, request.UserId, request.RoleId, ct))
            return null;

        if (await _repository.ExistsAsync(tenantId, request.UserId, request.RoleId, ct))
        {
            NotifyConflict();
            return null;
        }

        var entity = new UserRolesEntity(tenantId, request.UserId, request.RoleId);
        try
        {
            if (!await _domain.CreateAsync(entity, ct))
            {
                if (!_notify.HasNotify())
                    NotifyUnavailableParents();
                return null;
            }

            var created = await _repository.GetByIdAsync(tenantId, request.UserId, request.RoleId, ct);
            return UserRoleMapper.ToResponse(created ?? entity);
        }
        catch (UserRoleConflictException)
        {
            NotifyConflict();
            return null;
        }
    }

    private void NotifyConflict() =>
        _notify.Add(_localization.GetMessage("Application.Service.UserRole.Create.ResourceAlreadyExists"), 409);

    private void NotifyUnavailableParents() =>
        _notify.Add(_localization.GetMessage("Application.Service.UserRole.GetById.ResourceNotFound"), 404);

    private async Task<bool> ValidateParentsAsync(int tenantId, int userId, int roleId, CancellationToken ct)
    {
        if (userId <= 0 || roleId <= 0)
        {
            _notify.Add(_localization.GetMessage(userId <= 0 ? "Domain.User.InvalidId" : "Domain.Role.InvalidId"), 400);
            return false;
        }

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null || user.TenantId != tenantId || user.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.User.GetById.ResourceNotFound"), 404);
            return false;
        }

        var role = await _roleRepository.GetByIdAsync(roleId, ct);
        if (role is null || role.TenantId != tenantId || role.IsDeleted || !role.IsActive)
        {
            _notify.Add(_localization.GetMessage("Application.Service.UserRole.Create.RoleNotFound"), 404);
            return false;
        }

        return true;
    }

    public async Task DeleteAsync(int userId, int roleId, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        if (!await _domain.DeleteAsync(tenantId, userId, roleId, ct))
            _notify.Add(_localization.GetMessage("Application.Service.UserRole.Delete.ResourceNotFound"), 410);
    }


    public async Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct)
    {
        if (!_fileValidation.ValidateFile(file))
            return false;

        var tenantId = _currentUser.GetTenantId();
        var entities = new List<UserRolesEntity>();
        var keys = new HashSet<(int UserId, int RoleId)>();
        try
        {
            using var reader = new StreamReader(file.OpenReadStream(), new System.Text.UTF8Encoding(true, true), false);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                DetectColumnCountChanges = true,
                IgnoreBlankLines = false,
                ExceptionMessagesContainRawData = false
            });
            if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord is not ["UserId", "RoleId"])
            {
                _notify.Add(_localization.GetMessage("Application.Service.UserRole.ReadCsvFile.Exception"), 400);
                return false;
            }

            var maxRows = DomainExtensions.GetMaxCsvRows();
            while (csv.Read())
            {
                ct.ThrowIfCancellationRequested();
                if (entities.Count == maxRows)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.UserRole.ReadCsvFile.MaxRows", maxRows), 400);
                    return false;
                }

                if (!int.TryParse(csv.GetField(0), NumberStyles.None, CultureInfo.InvariantCulture, out var userId) || userId <= 0)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.UserRole.ValidateBulkItem.UserId", csv.Parser.Row), 400);
                    return false;
                }
                if (!int.TryParse(csv.GetField(1), NumberStyles.None, CultureInfo.InvariantCulture, out var roleId) || roleId <= 0)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.UserRole.ValidateBulkItem.RoleId", csv.Parser.Row), 400);
                    return false;
                }

                if (!keys.Add((userId, roleId)))
                {
                    NotifyConflict();
                    return false;
                }
                entities.Add(new UserRolesEntity(tenantId, userId, roleId));
            }
        }
        catch (Exception exception) when (exception is CsvHelperException or System.Text.DecoderFallbackException)
        {
            _notify.Add(_localization.GetMessage("Application.Service.UserRole.ReadCsvFile.Exception"), 400);
            return false;
        }

        if (entities.Count == 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.UserRole.BulkUpload.EmptyFile"), 400);
            return false;
        }

        foreach (var entity in entities)
        {
            if (!await ValidateParentsAsync(tenantId, entity.UserId, entity.RoleId, ct))
                return false;
            if (await _repository.ExistsAsync(tenantId, entity.UserId, entity.RoleId, ct))
            {
                NotifyConflict();
                return false;
            }
        }

        try
        {
            if (await _repository.CreateManyAsync(entities, ct))
                return true;

            NotifyUnavailableParents();
            return false;
        }
        catch (UserRoleConflictException)
        {
            NotifyConflict();
            return false;
        }
    }
}
