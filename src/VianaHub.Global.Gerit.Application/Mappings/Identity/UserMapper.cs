using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.User;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Identity;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class UserMapper
{
    [MapProperty(nameof(UserEntity.Email), nameof(UserResponse.Email))]
    public static partial UserResponse ToResponse(UserEntity source);
    [MapProperty(nameof(UserEntity.Email), nameof(UserDetailResponse.Email))]
    [MapProperty(nameof(UserEntity.UrlImage), nameof(UserDetailResponse.UrlImage))]
    public static partial UserDetailResponse ToDetail(UserEntity source);

    public static IEnumerable<UserResponse> ToResponses(IEnumerable<UserEntity> source) => source.Select(ToResponse);

    public static ListPageResponse<UserResponse> ToPaged(ListPage<UserEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);
}
