using System.Text.Json;
using System.Text.Json.Serialization;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.User;
using VianaHub.Global.Gerit.Application.Mappings.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Tests.Users;

public sealed class UserMappingTests
{
    internal static readonly string[] ListFields = ["Id", "Name", "Email", "PhoneNumber", "LastAccessAt", "IsActive"];
    internal static readonly string[] DetailFields = [.. ListFields, "UrlImage", "IsDeleted", "CreatedBy", "CreatedAt", "ModifiedBy", "ModifiedAt"];

    internal static UserEntity CreateEntity()
    {
        var entity = new UserEntity(7, "Example", "user@example.invalid", "private-password-hash", "+351000000000", 1);
        typeof(UserEntity).GetProperty(nameof(UserEntity.Id))!.SetValue(entity, 23);
        typeof(UserEntity).GetProperty(nameof(UserEntity.UrlImage))!.SetValue(entity, "https://example.invalid/avatar.png");
        entity.UpdateLastAccess();
        entity.Update("Updated example", "+351111111111", 2);
        return entity;
    }

    [Fact]
    public void ReadContracts_ExposeExactlyTheirSpecifiedProperties()
    {
        Assert.Equal(ListFields.Order(), typeof(UserResponse).GetProperties().Select(property => property.Name).Order());
        Assert.Equal(DetailFields.Order(), typeof(UserDetailResponse).GetProperties().Select(property => property.Name).Order());
    }

    [Fact]
    public void ReadContracts_PreserveNullFieldsUnderApiSerializationOptions()
    {
        var entity = new UserEntity(7, null, null, "private-hash", null, 1);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        foreach (var (response, fields) in new (object, string[])[]
                 { (UserMapper.ToResponse(entity), ListFields), (UserMapper.ToDetail(entity), DetailFields) })
        {
            var json = JsonSerializer.SerializeToElement(response, options);
            Assert.Equal(fields.Select(JsonNamingPolicy.CamelCase.ConvertName).Order(),
                json.EnumerateObject().Select(property => property.Name).Order());
            foreach (var property in response.GetType().GetProperties().Where(property => property.GetValue(response) is null))
                Assert.Equal(JsonValueKind.Null, json.GetProperty(JsonNamingPolicy.CamelCase.ConvertName(property.Name)).ValueKind);
        }
        var page = UserMapper.ToPaged(new ListPage<UserEntity>([entity], 1, 10, 1, 1));
        var item = Assert.Single(JsonSerializer.SerializeToElement(page, options).GetProperty("items").EnumerateArray());
        Assert.Equal(ListFields.Select(JsonNamingPolicy.CamelCase.ConvertName).Order(), item.EnumerateObject().Select(property => property.Name).Order());
    }

    [Fact]
    public void ReadMappings_PreserveAllContractValuesAndPagination()
    {
        var entity = CreateEntity();
        var list = UserMapper.ToResponse(entity);
        var detail = UserMapper.ToDetail(entity);
        foreach (var (response, fields) in new (object, string[])[] { (list, ListFields), (detail, DetailFields) })
        {
            Assert.Equal(fields.Order(), JsonSerializer.SerializeToElement(response).EnumerateObject().Select(property => property.Name).Order());
            foreach (var field in fields)
                Assert.Equal(typeof(UserEntity).GetProperty(field)!.GetValue(entity), response.GetType().GetProperty(field)!.GetValue(response));
        }

        Assert.Equal(entity.Email, Assert.Single(UserMapper.ToResponses([entity])).Email);
        var page = UserMapper.ToPaged(new ListPage<UserEntity>([entity], 2, 3, 7, 3));
        Assert.Equal(entity.Email, Assert.Single(page.Items).Email);
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(3, page.PageSize);
        Assert.Equal(7, page.TotalItems);
        Assert.Equal(3, page.TotalPages);
        var json = JsonSerializer.SerializeToElement(page, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(new[] { "items", "totalItems", "pageNumber", "pageSize", "totalPages" }.Order(), json.EnumerateObject().Select(property => property.Name).Order());
    }

    [Fact]
    public void UserResponses_ExposeProfileWithoutCredentialFields()
    {
        var hash = Guid.NewGuid().ToString("N");
        var entity = new UserEntity(7, "Example", "user@example.invalid", hash, "+351000000000", 1);
        var list = UserMapper.ToResponse(entity);
        var detail = UserMapper.ToDetail(entity);

        Assert.Equal(entity.Name, list.Name);
        Assert.Equal(entity.Name, detail.Name);
        Assert.Equal(entity.PhoneNumber, detail.PhoneNumber);
        Assert.Equal(entity.IsActive, detail.IsActive);

        foreach (var json in new[] { JsonSerializer.SerializeToElement(list), JsonSerializer.SerializeToElement(detail) })
        {
            Assert.DoesNotContain(json.EnumerateObject(),
                property => property.Name.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                            property.Value.ValueKind == JsonValueKind.String && property.Value.GetString() == hash);
        }
    }
}
