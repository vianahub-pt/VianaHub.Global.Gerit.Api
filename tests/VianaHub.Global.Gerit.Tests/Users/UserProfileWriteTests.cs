using System.Text.Json;
using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.User;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Exceptions;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Tests.Users;

public sealed class UserProfileWriteTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("{\"name\":\"Updated\"}", false)]
    [InlineData("{\"name\":\"Updated\",\"email\":null,\"phoneNumber\":null,\"urlImage\":null}", true)]
    public void UpdateRequest_DistinguishesOmittedAndExplicitNullAndHidesFlags(string json, bool provided)
    {
        var request = JsonSerializer.Deserialize<UpdateUserRequest>(json, JsonOptions)!;
        Assert.Equal(provided, request.EmailProvided);
        Assert.Equal(provided, request.PhoneNumberProvided);
        Assert.Equal(provided, request.UrlImageProvided);
        var output = JsonSerializer.SerializeToElement(request, JsonOptions);
        Assert.Equal(new[] { "email", "name", "phoneNumber", "urlImage" }, output.EnumerateObject().Select(property => property.Name).Order());
    }

    [Fact]
    public void ProfileUpdate_NormalizesEmailResetsChangedConfirmationsAndPreservesOtherState()
    {
        var user = Entity();
        user.ConfirmEmail(1);
        user.ConfirmPhoneNumber(1);
        var createdAt = user.CreatedAt;
        user.Update("Casing", "OWNER@EXAMPLE.INVALID", user.PhoneNumber, user.UrlImage, 2);
        Assert.True(user.EmailConfirmed);
        Assert.True(user.PhoneNumberConfirmed);
        user.Update("Changed", "other@example.invalid", null, null, 3);
        Assert.Equal("OTHER@EXAMPLE.INVALID", user.NormalizedEmail);
        Assert.False(user.EmailConfirmed);
        Assert.False(user.PhoneNumberConfirmed);
        Assert.Null(user.PhoneNumber);
        Assert.Null(user.UrlImage);
        Assert.Equal("private-hash", user.PasswordHash);
        Assert.Equal(createdAt, user.CreatedAt);
        Assert.Equal(1, user.CreatedBy);
        Assert.Equal(7, user.TenantId);
        Assert.True(user.IsActive);
        Assert.False(user.IsDeleted);
        Assert.Equal(3, user.ModifiedBy);
    }

    [Fact]
    public void LegacyUpdate_PreservesEmailAndImage()
    {
        var user = Entity();
        user.Update("Legacy", "new-phone", 3);
        Assert.Equal("owner@example.invalid", user.Email);
        Assert.Equal("https://example.invalid/avatar.png", user.UrlImage);
    }

    [Theory]
    [InlineData("{\"name\":\"Updated\"}", "+351111", "https://example.invalid/avatar.png")]
    [InlineData("{\"name\":\"Updated\",\"phoneNumber\":null,\"urlImage\":null}", null, null)]
    [InlineData("{\"name\":\"Updated\",\"phoneNumber\":\"new\",\"urlImage\":\"https://example.invalid/new.png\"}", "new", "https://example.invalid/new.png")]
    public async Task UpdateAsync_PreservesOmittedFieldsAndAppliesExplicitValues(string json, string? phone, string? image)
    {
        var fixture = new Fixture();
        fixture.Repository.Setup(repo => repo.ExistsByEmailAsync("owner@example.invalid", 23, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        fixture.Domain.Setup(domain => domain.UpdateAsync(fixture.User, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Assert.True(await fixture.Service.UpdateAsync(23, JsonSerializer.Deserialize<UpdateUserRequest>(json, JsonOptions)!, default));
        Assert.Equal(phone, fixture.User.PhoneNumber);
        Assert.Equal(image, fixture.User.UrlImage);
        Assert.Equal("owner@example.invalid", fixture.User.Email);
        Assert.Equal("private-hash", fixture.User.PasswordHash);
    }

    [Fact]
    public async Task UpdateAsync_DuplicatePrecheckDoesNotMutateAnyProfileField()
    {
        var fixture = new Fixture();
        fixture.Repository.Setup(repo => repo.ExistsByEmailAsync("duplicate@example.invalid", 23, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Assert.False(await fixture.Service.UpdateAsync(23, new UpdateUserRequest { Name = "Changed", Email = "duplicate@example.invalid", PhoneNumber = null, UrlImage = null }, default));
        Assert.Equal("Owner", fixture.User.Name);
        Assert.Equal("owner@example.invalid", fixture.User.Email);
        Assert.Equal("+351111", fixture.User.PhoneNumber);
        Assert.Equal("https://example.invalid/avatar.png", fixture.User.UrlImage);
        Assert.Equal(System.Net.HttpStatusCode.Conflict, fixture.Notifications.GetStatusCode());
        fixture.Domain.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ConcurrentEmailConflictBecomesLocalized409()
    {
        var fixture = new Fixture();
        fixture.Repository.Setup(repo => repo.ExistsByEmailAsync("other@example.invalid", 23, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        fixture.Domain.Setup(domain => domain.UpdateAsync(fixture.User, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UserEmailConflictException(new Exception("technical persistence detail")));
        Assert.False(await fixture.Service.UpdateAsync(23, new UpdateUserRequest { Name = "Updated", Email = "other@example.invalid" }, default));
        Assert.Equal(System.Net.HttpStatusCode.Conflict, fixture.Notifications.GetStatusCode());
        Assert.Equal("localized duplicate", Assert.Single(fixture.Notifications.GetErrorMessage()));
    }

    [Fact]
    public async Task CreateAsync_PersistsPhoneImageAndHashedPasswordInCurrentTenant()
    {
        var fixture = new Fixture();
        fixture.Repository.Setup(repo => repo.ExistsByEmailAsync("new@example.invalid", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        UserEntity? created = null;
        fixture.Domain.Setup(domain => domain.CreateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .Callback<UserEntity, CancellationToken>((entity, _) => { created = entity; typeof(UserEntity).GetProperty("Id")!.SetValue(entity, 31); })
            .ReturnsAsync(true);
        Assert.Equal(31, await fixture.Service.CreateAsync(new CreateUserRequest
        {
            Name = "New",
            Email = "new@example.invalid",
            Password = "Synthetic1!",
            ConfirmPassword = "Synthetic1!",
            PhoneNumber = "+351222",
            UrlImage = "https://example.invalid/new.png"
        }, default));
        Assert.NotNull(created);
        Assert.Equal("+351222", created.PhoneNumber);
        Assert.Equal("https://example.invalid/new.png", created.UrlImage);
        Assert.Equal(7, created.TenantId);
        Assert.True(DomainExtensions.VerifyClientSecret(created.PasswordHash, "Synthetic1!"));
    }

    private static UserEntity Entity()
    {
        var user = new UserEntity(7, "Owner", "owner@example.invalid", "private-hash", "+351111", 1, "https://example.invalid/avatar.png");
        typeof(UserEntity).GetProperty("Id")!.SetValue(user, 23);
        return user;
    }

    private sealed class Fixture
    {
        public UserEntity User { get; } = Entity();
        public Mock<IUserDataRepository> Repository { get; } = new(MockBehavior.Strict);
        public Mock<IUserDomainService> Domain { get; } = new(MockBehavior.Strict);
        public Notify Notifications { get; } = new();
        public UserAppService Service { get; }
        public Fixture()
        {
            Repository.Setup(repo => repo.GetByIdAsync(23, It.IsAny<CancellationToken>())).ReturnsAsync(User);
            var current = new Mock<ICurrentUserService>();
            current.Setup(value => value.GetTenantId()).Returns(7);
            current.Setup(value => value.GetUserId()).Returns(2);
            var localization = new Mock<ILocalizationService>();
            localization.Setup(value => value.GetMessage("Application.Service.User.Update.ResourceAlreadyExists")).Returns("localized duplicate");
            Service = new UserAppService(Repository.Object, Domain.Object, Notifications, current.Object, localization.Object, Mock.Of<IFileValidationService>());
        }
    }
}
