using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitContactPersons;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class VisitContactPersonMapper
{
    public static partial VisitContactPersonResponse ToResponse(VisitContactPersonsEntity source);
    public static partial VisitContactPersonDetailResponse ToDetail(VisitContactPersonsEntity source);
    public static IEnumerable<VisitContactPersonResponse> ToResponses(IEnumerable<VisitContactPersonsEntity> source) => source.Select(ToResponse);
    public static ListPageResponse<VisitContactPersonResponse> ToPaged(ListPage<VisitContactPersonsEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);
}
