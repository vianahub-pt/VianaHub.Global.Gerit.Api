using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Job;
using VianaHub.Global.Gerit.Application.Dtos.Response.Job;
using VianaHub.Global.Gerit.Domain.ReadModels;

namespace VianaHub.Global.Gerit.Application.Interfaces.Job;

public interface IJobAppService
{
    Task<JobDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<JobResponse>> GetPagedAsync(JobPagedFilter request, CancellationToken ct);
    Task<int> CreateAsync(CreateJobRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateJobRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> ExecuteAsync(int id, CancellationToken ct);
}
