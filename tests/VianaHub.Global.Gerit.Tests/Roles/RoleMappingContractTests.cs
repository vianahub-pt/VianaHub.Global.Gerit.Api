using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Tests.Roles;

public sealed class RoleMappingContractTests
{
    [Fact]
    public void RoleNameIndexUsesCanonicalFilteredContract()
    {
        var options = new DbContextOptionsBuilder<GeritDbContext>()
            .UseInMemoryDatabase($"roles-mapping-{Guid.NewGuid():N}")
            .Options;

        using var context = new GeritDbContext(options);
        var roleType = context.Model.FindEntityType(typeof(RoleEntity));
        var index = Assert.Single(roleType!.GetIndexes(), x => x.GetDatabaseName() == "UX_Roles_Tenant_Name");

        Assert.True(index.IsUnique);
        Assert.Equal([nameof(RoleEntity.TenantId), nameof(RoleEntity.Name)], index.Properties.Select(x => x.Name));
        Assert.Equal("[IsDeleted] = 0", index.GetFilter());
    }
}
