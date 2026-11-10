using VianaHub.Global.Gerit.Application.Mappings.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Tests.UserRoles;

public sealed class UserRoleMappingTests
{
    [Fact]
    public void MissingNavigations_PreserveIdentifiersAndReturnEmptyNames()
    {
        var association = new UserRolesEntity(1, 2, 3);
        var list = UserRoleMapper.ToResponse(association);
        var detail = UserRoleMapper.ToDetail(association);
        Assert.Equal((association.Id, 2, 3, "", ""), (list.Id, list.UserId, list.RoleId, list.UserName, list.RoleName));
        Assert.Equal((association.Id, 2, 3, "", ""), (detail.Id, detail.UserId, detail.RoleId, detail.UserName, detail.RoleName));
    }
}
