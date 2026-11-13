using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VianaHub.Global.Gerit.Api.Endpoints.Identity;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Application.Mappings.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Tests.Infrastructure;

namespace VianaHub.Global.Gerit.Tests.Users;

public sealed class UserReadContractHttpTests
{
    [Fact]
    public async Task ReadRoutes_ReturnExactListAndDetailContractsAndPreservePageEnvelope()
    {
        var entity = UserMappingTests.CreateEntity();
        var service = new Mock<IUserAppService>(MockBehavior.Strict);
        service.Setup(value => value.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserMapper.ToResponses([entity]));
        service.Setup(value => value.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserMapper.ToDetail(entity));
        service.Setup(value => value.GetPagedAsync(It.Is<PagedFilterRequest>(filter => filter.PageNumber == 2 && filter.PageSize == 3), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserMapper.ToPaged(new ListPage<UserEntity>([entity], 2, 3, 7, 3)));

        // Exercita os endpoints e a serialização reais sem dependência de SQL Server.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(service.Object);
        builder.Services.AddSingleton(Mock.Of<ILocalizationService>());
        builder.Services.AddScoped<INotify, Notify>();
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapUserEndpoints();
        await app.StartAsync();
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-Permission-Resource", "Users");
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "GetAll,GetBy,GetPaged");

        var list = await GetAsync(client, "/v1/users/");
        var listItem = Assert.Single(list.EnumerateArray());
        AssertFields(listItem, UserMappingTests.ListFields);
        Assert.Equal(entity.Email, listItem.GetProperty("email").GetString());
        var page = await GetAsync(client, "/v1/users/paged?PageNumber=2&PageSize=3");
        AssertFields(page, ["Items", "TotalItems", "PageNumber", "PageSize", "TotalPages"]);
        var pagedItem = Assert.Single(page.GetProperty("items").EnumerateArray());
        AssertFields(pagedItem, UserMappingTests.ListFields);
        Assert.Equal(entity.Email, pagedItem.GetProperty("email").GetString());
        Assert.Equal(7, page.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, page.GetProperty("pageNumber").GetInt32());
        Assert.Equal(3, page.GetProperty("pageSize").GetInt32());
        Assert.Equal(3, page.GetProperty("totalPages").GetInt32());
        var detail = await GetAsync(client, $"/v1/users/{entity.Id}");
        AssertFields(detail, UserMappingTests.DetailFields);
        Assert.Equal(entity.Email, detail.GetProperty("email").GetString());
        Assert.Equal(entity.UrlImage, detail.GetProperty("urlImage").GetString());
        service.VerifyAll();
    }

    private static void AssertFields(JsonElement value, string[] fields) =>
        Assert.Equal(fields.Select(JsonNamingPolicy.CamelCase.ConvertName).Order(),
            value.EnumerateObject().Select(property => property.Name).Order());

    private static async Task<JsonElement> GetAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
