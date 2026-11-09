using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.User;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Tests.Users;

public class UserPasswordAuthorizationTests
{
    [Theory]
    [InlineData(17, 18)]
    [InlineData(0, 0)]
    public async Task UpdatePassword_RejectsUnauthorizedTargetBeforeLookup(int currentUserId, int targetId)
    {
        const string message = "Só pode alterar a sua própria palavra-passe";
        var repository = new Mock<IUserDataRepository>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(user => user.GetUserId()).Returns(currentUserId);
        var localization = new Mock<ILocalizationService>();
        localization.Setup(service => service.GetMessage("Application.Service.User.UpdatePassword.OtherUser"))
            .Returns(message);
        var notifications = new Mock<INotify>();
        var service = new UserAppService(
            repository.Object,
            Mock.Of<IUserDomainService>(),
            notifications.Object,
            currentUser.Object,
            localization.Object,
            Mock.Of<IFileValidationService>());

        var result = await service.UpdatePasswordAsync(targetId, new UpdatePasswordRequest(), CancellationToken.None);

        Assert.False(result);
        notifications.Verify(notify => notify.Add(message, 403), Times.Once);
        repository.VerifyNoOtherCalls();
    }
}
