using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Tests.IdentityMappings;

public sealed class IsActiveIdentityEntityTests
{
    [Fact]
    public void ActionCreateAndUpdateHonorOptionalState()
    {
        var defaults = new ActionEntity("read", "Read access", 17);
        var entity = new ActionEntity("write", "Write access", 17, false);
        Assert.True(defaults.IsActive);
        Assert.False(entity.IsActive);

        entity.Update("Updated", 18);
        Assert.False(entity.IsActive);
        entity.Update("Updated", 18, true);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void ResourceCreateAndUpdateHonorOptionalState()
    {
        var defaults = new ResourceEntity("clients", "Clients", 17);
        var entity = new ResourceEntity("billing", "Billing", 17, false);
        Assert.True(defaults.IsActive);
        Assert.False(entity.IsActive);

        entity.Update("Updated", 18);
        Assert.False(entity.IsActive);
        entity.Update("Updated", 18, true);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void RoleCreateAndUpdateHonorOptionalState()
    {
        var defaults = new RoleEntity(7, "operator", "Operator", 17);
        var entity = new RoleEntity(7, "reviewer", "Reviewer", 17, false);
        Assert.True(defaults.IsActive);
        Assert.False(entity.IsActive);

        entity.Update("Updated", 18);
        Assert.False(entity.IsActive);
        entity.Update("Updated", 18, true);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void UserCreateAndUpdateHonorOptionalState()
    {
        var defaults = new UserEntity(7, "Default", "default@example.test", "hash", null, 17);
        var entity = new UserEntity(7, "Inactive", "inactive@example.test", "hash", null, 17, null, false);
        Assert.True(defaults.IsActive);
        Assert.False(entity.IsActive);

        entity.Update("Inactive", "inactive@example.test", null, null, 18);
        Assert.False(entity.IsActive);
        entity.Update("Inactive", "inactive@example.test", null, null, 18, true);
        Assert.True(entity.IsActive);
    }
}
