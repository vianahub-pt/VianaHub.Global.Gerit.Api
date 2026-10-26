using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Infra.Data.Context;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VianaHub.Global.Gerit.Api.Filters;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Action;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Resource;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Role;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Action;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Resource;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Role;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Tests.Domain.Identity;

public class AuthorizationIdentityTests
{
    [Theory]
    [InlineData(typeof(ActionEntity))]
    [InlineData(typeof(ResourceEntity))]
    [InlineData(typeof(RoleEntity))]
    [InlineData(typeof(CreateActionRequest))]
    [InlineData(typeof(UpdateActionRequest))]
    [InlineData(typeof(BulkUploadActionItem))]
    [InlineData(typeof(CreateResourceRequest))]
    [InlineData(typeof(UpdateResourceRequest))]
    [InlineData(typeof(BulkUploadResourceItem))]
    [InlineData(typeof(CreateRoleRequest))]
    [InlineData(typeof(UpdateRoleRequest))]
    [InlineData(typeof(BulkUploadRoleItem))]
    [InlineData(typeof(ActionResponse))]
    [InlineData(typeof(ActionDetailResponse))]
    [InlineData(typeof(ResourceResponse))]
    [InlineData(typeof(ResourceDetailResponse))]
    [InlineData(typeof(RoleResponse))]
    [InlineData(typeof(RoleDetailResponse))]
    public void IdentityContracts_DoNotExposeCode(Type type)
    {
        Assert.Null(type.GetProperty("Code"));
    }

    [Fact]
    public void Update_PreservesSemanticNamesAndPermissionIds()
    {
        var action = new ActionEntity("GetBy", "Read", 1);
        var resource = new ResourceEntity("Actions", "Actions", 1);
        var role = new RoleEntity(1, "Admin", "Administrator", 1);
        var permission = new RolePermissionsEntity(1, 2, 3, 4);

        action.Update("Changed", 2);
        resource.Update("Changed", 2);
        role.Update("Changed", 2);

        Assert.Equal("GetBy", action.Name);
        Assert.Equal("Actions", resource.Name);
        Assert.Equal("Admin", role.Name);
        Assert.Equal("Changed", action.Description);
        Assert.Equal("Changed", resource.Description);
        Assert.Equal("Changed", role.Description);
        Assert.Equal(2, permission.RoleId);
        Assert.Equal(3, permission.ResourceId);
        Assert.Equal(4, permission.ActionId);
        Assert.Null(typeof(UpdateActionRequest).GetProperty("Name"));
        Assert.Null(typeof(UpdateResourceRequest).GetProperty("Name"));
        Assert.Null(typeof(UpdateRoleRequest).GetProperty("Name"));
    }

    [Fact]
    public async Task IdentityEntities_PersistWithoutCodeAndKeepIdRelationships()
    {
        var options = new DbContextOptionsBuilder<GeritDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new GeritDbContext(options);
        var action = new ActionEntity("GetBy", "Read", 1);
        var resource = new ResourceEntity("Actions", "Actions", 1);
        var role = new RoleEntity(1, "Admin", "Administrator", 1);
        context.AddRange(action, resource, role);
        await context.SaveChangesAsync();
        var permission = new RolePermissionsEntity(1, role.Id, resource.Id, action.Id);
        context.Add(permission);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.Equal("GetBy", (await context.Actions.SingleAsync()).Name);
        Assert.Equal("Actions", (await context.Resources.SingleAsync()).Name);
        Assert.Equal("Admin", (await context.Roles.SingleAsync()).Name);
        var persisted = await context.RolePermissions.SingleAsync();
        Assert.Equal(action.Id, persisted.ActionId);
        Assert.Equal(resource.Id, persisted.ResourceId);
        Assert.Equal(role.Id, persisted.RoleId);
        foreach (var type in new[] { typeof(ActionEntity), typeof(ResourceEntity), typeof(RoleEntity) })
        {
            Assert.Null(context.Model.FindEntityType(type)!.FindProperty("Code"));
        }
    }

    [Fact]
    public async Task Authorization_AcceptsExistingRoleAndPermissionsClaims()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("permissions", "{\"actions\":[\"getby\"]}")
        }, "Bearer"));
        var filter = new AuthorizationFilter("Admin", "Actions", "GetBy",
            NullLogger<AuthorizationFilter>.Instance, Mock.Of<ILocalizationService>());
        var invocation = new DefaultEndpointFilterInvocationContext(context);
        var result = await filter.InvokeAsync(invocation, _ => ValueTask.FromResult<object?>("authorized"));
        Assert.Equal("authorized", result);
    }
}
