using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Job;
using VianaHub.Global.Gerit.Domain.Entities.Job;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Job;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class JobMapper
{
    [MapProperty(nameof(JobDefinitionsEntity.ModifiedAt), nameof(JobResponse.UpdatedAt))]
    [MapperIgnoreTarget(nameof(JobResponse.Status))]
    [MapperIgnoreTarget(nameof(JobResponse.NextExecution))]
    [MapperIgnoreTarget(nameof(JobResponse.LastExecution))]
    [MapperIgnoreTarget(nameof(JobResponse.LastExecutionStatus))]
    public static partial JobResponse ToResponse(JobDefinitionsEntity source);

    [MapProperty(nameof(JobDefinitionsEntity.ModifiedAt), nameof(JobDetailResponse.UpdatedAt))]
    [MapperIgnoreTarget(nameof(JobDetailResponse.Status))]
    [MapperIgnoreTarget(nameof(JobDetailResponse.NextExecution))]
    [MapperIgnoreTarget(nameof(JobDetailResponse.LastExecution))]
    [MapperIgnoreTarget(nameof(JobDetailResponse.LastExecutionStatus))]
    public static partial JobDetailResponse ToDetail(JobDefinitionsEntity source);

    public static ListPageResponse<JobResponse> ToPaged(ListPage<JobDefinitionsEntity> source) =>
        new(source.Items.Select(ToResponse).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);
}
