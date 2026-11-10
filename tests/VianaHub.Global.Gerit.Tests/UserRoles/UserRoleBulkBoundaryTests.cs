using System.Text;
using Microsoft.AspNetCore.Http;
using Moq;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Tests.UserRoles;

public sealed class UserRoleBulkBoundaryTests
{
    [Theory]
    [InlineData(10_000, true)]
    [InlineData(10_001, false)]
    public async Task BulkUpload_ChecksRowBoundaryBeforeParentLookup(int rows, bool reachesParentLookup)
    {
        var csv = "UserId;RoleId\n" + string.Concat(Enumerable.Range(1, rows).Select(id => $"{id};1\n"));
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var file = new FormFile(stream, 0, stream.Length, "file", "user-roles.csv");
        var fileValidation = new Mock<IFileValidationService>();
        fileValidation.Setup(service => service.ValidateFile(file)).Returns(true);
        var users = new Mock<IUserDataRepository>();
        var assignments = new Mock<IUserRoleDataRepository>(MockBehavior.Strict);
        var service = new UserRoleAppService(Mock.Of<IUserRoleDomainService>(), assignments.Object,
            Mock.Of<IRoleDataRepository>(), users.Object, Mock.Of<INotify>(), Mock.Of<ILocalizationService>(),
            Mock.Of<ICurrentUserService>(user => user.GetTenantId() == 7), fileValidation.Object);

        Assert.False(await service.BulkUploadAsync(file, CancellationToken.None));
        users.Verify(repository => repository.GetByIdAsync(1, It.IsAny<CancellationToken>()),
            reachesParentLookup ? Times.Once() : Times.Never());
        assignments.VerifyNoOtherCalls();
    }
}
