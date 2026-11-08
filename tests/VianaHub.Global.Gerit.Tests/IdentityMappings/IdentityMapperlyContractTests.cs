using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Mappings.Identity;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Tests.IdentityMappings;

public sealed class IdentityMapperlyContractTests
{
    [Fact]
    public void ActionMapperMapsResponseDetailAndPage()
    {
        var entity = new ActionEntity("Read", "Read description", 7);
        SetId(entity, 1);

        var response = ActionMapper.ToResponse(entity);
        var detail = ActionMapper.ToDetail(entity);
        var page = ActionMapper.ToPaged(new ListPage<ActionEntity>([entity], 2, 10, 1, 1));

        Assert.Equal(entity.Id, response.Id);
        Assert.Equal(entity.Description, detail.Description);
        Assert.Equal(2, page.PageNumber);
        Assert.Single(page.Items);
    }

    [Fact]
    public void ResourceMapperMapsResponseDetailAndPage()
    {
        var entity = new ResourceEntity("Clients", "Client resources", 7);
        SetId(entity, 2);

        Assert.Equal(entity.Name, ResourceMapper.ToResponse(entity).Name);
        Assert.Equal(entity.CreatedBy, ResourceMapper.ToDetail(entity).CreatedBy);
        Assert.Single(ResourceMapper.ToPaged(new ListPage<ResourceEntity>([entity], 1, 10, 1, 1)).Items);
    }

    [Fact]
    public void RoleMapperMapsResponseDetailAndPage()
    {
        var entity = new RoleEntity(1, "Admin", "Administrator", 7);
        SetId(entity, 3);

        Assert.Equal(entity.Name, RoleMapper.ToResponse(entity).Name);
        Assert.Equal(entity.IsDeleted, RoleMapper.ToDetail(entity).IsDeleted);
        Assert.Single(RoleMapper.ToPaged(new ListPage<RoleEntity>([entity], 1, 10, 1, 1)).Items);
    }

    [Fact]
    public void RolePermissionMapperPreservesNavigationNamesAndNullFallback()
    {
        var nullNavigation = new RolePermissionsEntity(1, 2, 3, 4);
        var nullResponse = RolePermissionMapper.ToResponse(nullNavigation);
        Assert.Equal(string.Empty, nullResponse.RoleName);
        Assert.Equal(string.Empty, nullResponse.ResourceName);
        Assert.Equal(string.Empty, nullResponse.ActionName);

        var populated = new RolePermissionsEntity(1, 2, 3, 4);
        SetProperty(populated, nameof(RolePermissionsEntity.Role), new RoleEntity(1, "Admin", null, 1));
        SetProperty(populated, nameof(RolePermissionsEntity.Resource), new ResourceEntity("Clients", null, 1));
        SetProperty(populated, nameof(RolePermissionsEntity.Action), new ActionEntity("Read", "Read", 1));
        var detail = RolePermissionMapper.ToDetail(populated);

        Assert.Equal("Admin", detail.RoleName);
        Assert.Equal("Clients", detail.ResourceName);
        Assert.Equal("Read", detail.ActionName);
    }

    [Fact]
    public void UserMapperMapsResponseDetailAndPage()
    {
        var entity = new UserEntity(1, "Alice", "alice@example.test", "hash", "123", 7);
        SetId(entity, 4);

        Assert.Equal(entity.PhoneNumber, UserMapper.ToResponse(entity).PhoneNumber);
        Assert.Equal(entity.LastAccessAt, UserMapper.ToDetail(entity).LastAccessAt);
        Assert.Single(UserMapper.ToPaged(new ListPage<UserEntity>([entity], 1, 10, 1, 1)).Items);
    }

    [Fact]
    public void UserRoleMapperPreservesNavigationNamesAndNullFallback()
    {
        var entity = new UserRolesEntity(1, 2, 3);
        var response = UserRoleMapper.ToResponse(entity);
        Assert.Equal(string.Empty, response.UserName);
        Assert.Equal(string.Empty, response.RoleName);

        SetProperty(entity, nameof(UserRolesEntity.User), new UserEntity(1, "Alice", "alice@example.test", "hash", null, 1));
        SetProperty(entity, nameof(UserRolesEntity.Role), new RoleEntity(1, "Admin", null, 1));
        var detail = UserRoleMapper.ToDetail(entity);
        Assert.Equal("Alice", detail.UserName);
        Assert.Equal("Admin", detail.RoleName);
    }

    private static void SetId(Entity entity, int id) => SetProperty(entity, nameof(Entity.Id), id);

    private static void SetProperty(object instance, string propertyName, object? value) =>
        instance.GetType().GetProperty(propertyName)!.SetValue(instance, value);
}
