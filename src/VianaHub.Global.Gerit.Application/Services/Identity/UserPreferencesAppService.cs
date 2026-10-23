using AutoMapper;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.UserPreferences;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.UserPreferences;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace VianaHub.Global.Gerit.Application.Services.Identity;

public class UserPreferencesAppService : IUserPreferencesAppService
{
    private readonly IUserPreferencesDataRepository _repo;
    private readonly IUserPreferencesDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UserPreferencesAppService> _logger;

    public UserPreferencesAppService(
        IUserPreferencesDataRepository repo,
        IUserPreferencesDomainService domain,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        ILogger<UserPreferencesAppService> logger)
    {
        _repo = repo;
        _domain = domain;
        _mapper = mapper;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IEnumerable<UserPreferencesResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<UserPreferencesResponse>>(entities);
    }

    public async Task<UserPreferencesDetailResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.UserPreferences.Update.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<UserPreferencesDetailResponse>(entity);
    }

    public async Task<UserPreferencesResponse> GetByUserAsync(int userId, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var entity = await _repo.GetByUserAsync(tenantId, userId, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.UserPreferences.Update.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<UserPreferencesResponse>(entity);
    }

    public async Task<ListPageResponse<UserPreferencesResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<UserPreferencesResponse>>(paged);
    }

    public async Task<int> CreateAsync(CreateUserPreferencesRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var userId = _currentUser.GetUserId();

        var exists = await _repo.ExistsByUserAsync(tenantId, userId, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.UserPreferences.Create.ResourceAlreadyExists"), 400);
            return 0;
        }

        if (!TimeSpan.TryParse(request.DayStart, out var dayStart))
            dayStart = TimeSpan.Parse("09:00");
        if (!TimeSpan.TryParse(request.DayEnd, out var dayEnd))
            dayEnd = TimeSpan.Parse("18:00");

        var entity = new UserPreferencesEntity(tenantId, userId, request.Appearance, request.CurrencyCode, request.Locale, request.Timezone, request.DateFormat, request.TimeFormat, dayStart, dayEnd, _currentUser.GetUserId());

        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int id, UpdateUserPreferencesRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.UserPreferences.Update.ResourceNotFound"), 410);
            return false;
        }

        if (!TimeSpan.TryParse(request.DayStart, out var dayStart))
            dayStart = entity.DayStart;
        if (!TimeSpan.TryParse(request.DayEnd, out var dayEnd))
            dayEnd = entity.DayEnd;

        entity.Update(request.Appearance, request.Locale, request.CurrencyCode, request.Timezone, request.DateFormat, request.TimeFormat, dayStart, dayEnd, request.EmailNewsletter, request.EmailWeeklyReport, request.EmailApproval, request.EmailAlerts, request.EmailReminders, request.EmailPlanner, _currentUser.GetUserId());

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.UserPreferences.Activate.ResourceNotFound"), 410);
            return false;
        }

        entity.Activate(_currentUser.GetUserId());
        return await _domain.ActivateAsync(entity, ct);
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.UserPreferences.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        entity.Deactivate(_currentUser.GetUserId());
        return await _domain.DeactivateAsync(entity, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.UserPreferences.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }
}
