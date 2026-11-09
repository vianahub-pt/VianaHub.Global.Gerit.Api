using System.Text.Json;
using VianaHub.Global.Gerit.Application.Mappings.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Tests.Users;

public sealed class UserMappingTests
{
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
