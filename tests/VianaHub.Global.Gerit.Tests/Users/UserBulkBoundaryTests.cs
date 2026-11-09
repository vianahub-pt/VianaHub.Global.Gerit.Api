using System.Text;
using Microsoft.AspNetCore.Http;
using Moq;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Tests.Users;

public class UserBulkBoundaryTests
{
    [Theory]
    [InlineData("")]
    [InlineData("Email;Name;Password;PhoneNumber\n")]
    [InlineData("Name;Email;Password;PhoneNumber\n\"Unclosed;user@example.invalid;Synthetic1!;\n")]
    [InlineData("Name;Email;Password;PhoneNumber\nValid;user@example.invalid;Synthetic1!;\nInvalid;broken;Synthetic1!;\n")]
    [InlineData("Name;Email;Password;PhoneNumber\nSample;user@example.invalid;weak;\n")]
    [InlineData("Name;Email;Password;PhoneNumber\nSample;user@example.invalid;Synthetic1!\n")]
    [InlineData("Name;Email;Password;PhoneNumber\nSample;user@example.invalid;Synthetic1!;=cmd|test\n")]
    [InlineData("Name;Email;Password;PhoneNumber\nSample;user@example.invalid;Synthetic1!;+1+1\n")]
    public async Task BulkUpload_RejectsInvalidFileBeforeAnyPersistence(string content)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "file", "users.csv");
        var validation = new Mock<IFileValidationService>();
        validation.Setup(service => service.ValidateFile(file)).Returns(true);
        var repository = new Mock<IUserDataRepository>(MockBehavior.Strict);
        var domain = new Mock<IUserDomainService>(MockBehavior.Strict);
        var service = new UserAppService(
            repository.Object, domain.Object, Mock.Of<INotify>(), Mock.Of<ICurrentUserService>(),
            Mock.Of<ILocalizationService>(), validation.Object);

        Assert.False(await service.BulkUploadAsync(file, CancellationToken.None));

        repository.VerifyNoOtherCalls();
        domain.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(10_000, true)]
    [InlineData(10_001, false)]
    public async Task BulkUpload_EnforcesRowLimitBeforeCheckingDatabase(int rows, bool reachesDatabase)
    {
        var content = "Name;Email;Password;PhoneNumber\n" +
            string.Concat(Enumerable.Range(0, rows).Select(index => $"Sample;user{index}@example.invalid;Synthetic1!;\n"));
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "file", "users.csv");
        var validation = new Mock<IFileValidationService>();
        validation.Setup(service => service.ValidateFile(file)).Returns(true);
        var repository = new Mock<IUserDataRepository>();
        repository.Setup(repo => repo.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var domain = new Mock<IUserDomainService>(MockBehavior.Strict);
        var service = new UserAppService(
            repository.Object, domain.Object, Mock.Of<INotify>(), Mock.Of<ICurrentUserService>(),
            Mock.Of<ILocalizationService>(), validation.Object);

        Assert.False(await service.BulkUploadAsync(file, CancellationToken.None));

        repository.Verify(repo => repo.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            reachesDatabase ? Times.Once() : Times.Never());
        domain.VerifyNoOtherCalls();
    }
}
