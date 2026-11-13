using FluentValidation;
using Moq;
using VianaHub.Global.Gerit.Api.Validators.Identity.User;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.User;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Validators.Identity.User;

namespace VianaHub.Global.Gerit.Tests.Users;

public sealed class UserProfileValidationTests
{
    private static ILocalizationService Localization()
    {
        var mock = new Mock<ILocalizationService>();
        mock.Setup(value => value.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        mock.Setup(value => value.GetMessage(It.IsAny<string>(), It.IsAny<object?[]>())).Returns((string key, object?[] _) => key);
        return mock.Object;
    }

    [Theory]
    [InlineData("https://example.invalid/avatar.png", true)]
    [InlineData("http://example.invalid/avatar.png", true)]
    [InlineData(null, true)]
    [InlineData("", false)]
    [InlineData("/avatar.png", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:image/png;base64,test", false)]
    [InlineData("ftp://example.invalid/avatar.png", false)]
    [InlineData("https://user:password@example.invalid/avatar.png", false)]
    [InlineData(" https://example.invalid/avatar.png", false)]
    public void ImageValidation_IsConsistentAcrossCreateUpdateAndDomain(string? image, bool valid)
    {
        var create = Create(); create.UrlImage = image;
        var update = new UpdateUserRequest { Name = "Valid", UrlImage = image };
        var user = Entity(image);
        Assert.Equal(valid, new CreateUserRouteValidator(Localization()).Validate(create).IsValid);
        Assert.Equal(valid, new UpdateUserRouteValidator(Localization()).Validate(update).IsValid);
        Assert.Equal(valid, new CreateUserValidator(Localization()).Validate(user).IsValid);
        Assert.Equal(valid, new UpdateUserValidator(Localization()).Validate(user).IsValid);
    }

    [Theory]
    [InlineData("Name", 150, true)]
    [InlineData("Name", 151, false)]
    [InlineData("PhoneNumber", 50, true)]
    [InlineData("PhoneNumber", 51, false)]
    [InlineData("Email", 256, true)]
    [InlineData("Email", 257, false)]
    [InlineData("UrlImage", 500, true)]
    [InlineData("UrlImage", 501, false)]
    public void LengthBoundaries_AreConsistentInRoutesAndDomain(string field, int length, bool valid)
    {
        var value = field switch
        {
            "Email" => new string('a', length - "@example.invalid".Length) + "@example.invalid",
            "UrlImage" => "https://example.invalid/" + new string('a', length - "https://example.invalid/".Length),
            _ => new string('a', length)
        };
        var create = Create();
        typeof(CreateUserRequest).GetProperty(field)!.SetValue(create, value);
        var update = new UpdateUserRequest { Name = create.Name, Email = create.Email, PhoneNumber = create.PhoneNumber, UrlImage = create.UrlImage };
        var user = new UserEntity(7, create.Name, create.Email, new string('x', 60), create.PhoneNumber, 1, create.UrlImage);
        typeof(UserEntity).GetProperty("Id")!.SetValue(user, 23);
        Assert.Equal(valid, new CreateUserRouteValidator(Localization()).Validate(create).IsValid);
        Assert.Equal(valid, new UpdateUserRouteValidator(Localization()).Validate(update).IsValid);
        Assert.Equal(valid, new CreateUserValidator(Localization()).Validate(user).IsValid);
        Assert.Equal(valid, new UpdateUserValidator(Localization()).Validate(user).IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Update_RejectsMissingNameAndExplicitEmptyEmail(string? value)
    {
        var validator = new UpdateUserRouteValidator(Localization());
        Assert.False(validator.Validate(new UpdateUserRequest { Name = value }).IsValid);
        Assert.False(validator.Validate(new UpdateUserRequest { Name = "Valid", Email = value }).IsValid);
        Assert.True(validator.Validate(new UpdateUserRequest { Name = "Valid" }).IsValid);
    }

    private static CreateUserRequest Create() => new()
    {
        Name = "Valid",
        Email = "user@example.invalid",
        Password = "Synthetic1!",
        ConfirmPassword = "Synthetic1!"
    };

    private static UserEntity Entity(string? image)
    {
        var user = new UserEntity(7, "Valid", "user@example.invalid", new string('x', 60), null, 1, image);
        typeof(UserEntity).GetProperty("Id")!.SetValue(user, 23);
        return user;
    }
}
