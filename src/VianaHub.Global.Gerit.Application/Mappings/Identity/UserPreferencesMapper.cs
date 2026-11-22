using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.UserPreferences;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Identity;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class UserPreferencesMapper
{
    public static UserPreferencesResponse ToResponse(UserPreferencesEntity source)
    {
        var response = MapResponse(source);
        response.Tenant = source.Tenant?.Name;
        response.User = source.User?.Name;
        response.DayStart = FormatTime(source.DayStart);
        response.DayEnd = FormatTime(source.DayEnd);
        return response;
    }

    public static UserPreferencesDetailResponse ToDetail(UserPreferencesEntity source)
    {
        var response = MapDetail(source);
        response.Tenant = source.Tenant?.Name;
        response.User = source.User?.Name;
        response.DayStart = FormatTime(source.DayStart);
        response.DayEnd = FormatTime(source.DayEnd);
        return response;
    }

    public static IEnumerable<UserPreferencesResponse> ToResponses(IEnumerable<UserPreferencesEntity> source) => source.Select(ToResponse);

    public static ListPageResponse<UserPreferencesResponse> ToPaged(ListPage<UserPreferencesEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    private static string FormatTime(TimeSpan value) => value.ToString(@"hh\:mm");

    [MapperIgnoreTarget(nameof(UserPreferencesResponse.Tenant))]
    [MapperIgnoreTarget(nameof(UserPreferencesResponse.User))]
    [MapperIgnoreTarget(nameof(UserPreferencesResponse.DayStart))]
    [MapperIgnoreTarget(nameof(UserPreferencesResponse.DayEnd))]
    private static partial UserPreferencesResponse MapResponse(UserPreferencesEntity source);

    [MapperIgnoreTarget(nameof(UserPreferencesDetailResponse.Tenant))]
    [MapperIgnoreTarget(nameof(UserPreferencesDetailResponse.User))]
    [MapperIgnoreTarget(nameof(UserPreferencesDetailResponse.DayStart))]
    [MapperIgnoreTarget(nameof(UserPreferencesDetailResponse.DayEnd))]
    private static partial UserPreferencesDetailResponse MapDetail(UserPreferencesEntity source);
}
