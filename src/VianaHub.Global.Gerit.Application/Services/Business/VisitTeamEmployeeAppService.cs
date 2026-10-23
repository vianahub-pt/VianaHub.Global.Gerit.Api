using AutoMapper;
using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeamEmployee;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamEmployee;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class VisitTeamEmployeeAppService : IVisitTeamEmployeeAppService
{
    private readonly IVisitTeamEmployeeDataRepository _repo;
    private readonly IVisitTeamEmployeeDomainService _domain;
    private readonly IEmployeeDataRepository _employeeRepository;
    private readonly IVisitTeamFunctionDataRepository _functionRepository;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;

    public VisitTeamEmployeeAppService(
        IVisitTeamEmployeeDataRepository repo,
        IVisitTeamEmployeeDomainService domain,
        IEmployeeDataRepository employeeRepository,
        IVisitTeamFunctionDataRepository functionRepository,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _domain = domain;
        _employeeRepository = employeeRepository;
        _functionRepository = functionRepository;
        _mapper = mapper;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<VisitTeamEmployeeResponse>> GetAllAsync(int visitTeamId, CancellationToken ct)
    {
        var entities = await _repo.GetByVisitTeamIdAsync(visitTeamId, ct);
        return _mapper.Map<IEnumerable<VisitTeamEmployeeResponse>>(entities);
    }

    public async Task<VisitTeamEmployeeDetailResponse> GetByIdAsync(int visitTeamId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted || entity.VisitTeamId != visitTeamId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEmployee.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<VisitTeamEmployeeDetailResponse>(entity);
    }

    public async Task<IEnumerable<VisitTeamEmployeeResponse>> GetActiveAsync(int visitTeamId, CancellationToken ct)
    {
        var entities = await _repo.GetActiveByVisitTeamIdAsync(visitTeamId, ct);
        return _mapper.Map<IEnumerable<VisitTeamEmployeeResponse>>(entities);
    }

    public async Task<ListPageResponse<VisitTeamEmployeeResponse>> GetPagedAsync(int visitTeamId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<VisitTeamEmployeeResponse>>(paged);
    }

    public async Task<int> CreateAsync(int visitTeamId, CreateVisitTeamEmployeeRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();

        if (!await _employeeRepository.ExistsByIdAsync(request.EmployeeId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEmployee.Create.EmployeeNotFound"), 400);
            return 0;
        }

        if (!await _functionRepository.ExistsByIdAsync(request.VisitTeamFunctionId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEmployee.Create.FunctionNotFound"), 400);
            return 0;
        }

        if (await _repo.ExistsActiveAssignmentAsync(visitTeamId, request.EmployeeId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEmployee.Create.AlreadyAssigned"), 409);
            return 0;
        }

        var entity = new VisitTeamEmployeeEntity(
            tenantId,
            visitTeamId,
            request.EmployeeId,
            request.VisitTeamFunctionId,
            request.IsLeader,
            request.StartDateTime,
            _currentUser.GetUserId()
        );

        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int visitTeamId, int id, UpdateVisitTeamEmployeeRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted || entity.VisitTeamId != visitTeamId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEmployee.Update.ResourceNotFound"), 410);
            return false;
        }

        if (!await _functionRepository.ExistsByIdAsync(request.VisitTeamFunctionId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEmployee.Update.FunctionNotFound"), 400);
            return false;
        }

        entity.Update(request.VisitTeamFunctionId, request.IsLeader, request.StartDateTime, request.EndDateTime, _currentUser.GetUserId());

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int visitTeamId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted || entity.VisitTeamId != visitTeamId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEmployee.Activate.ResourceNotFound"), 410);
            return false;
        }

        entity.Activate(_currentUser.GetUserId());
        return await _domain.ActivateAsync(entity, ct);
    }

    public async Task<bool> DeactivateAsync(int visitTeamId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted || entity.VisitTeamId != visitTeamId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEmployee.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        entity.Deactivate(_currentUser.GetUserId());
        return await _domain.DeactivateAsync(entity, ct);
    }

    public async Task<bool> DeleteAsync(int visitTeamId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted || entity.VisitTeamId != visitTeamId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEmployee.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<bool> BulkUploadAsync(int visitTeamId, IFormFile file, CancellationToken ct)
    {
        _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEmployee.BulkUpload.NotImplemented"), 501);
        return false;
    }
}
