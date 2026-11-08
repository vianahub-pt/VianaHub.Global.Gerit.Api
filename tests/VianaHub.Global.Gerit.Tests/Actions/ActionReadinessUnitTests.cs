using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Action;
using VianaHub.Global.Gerit.Application.Services.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Services.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Validators.Identity.Action;

namespace VianaHub.Global.Gerit.Tests.Actions;

public sealed class ActionReadinessUnitTests
{
    [Fact]
    public async Task Create_TrimsNameBeforeDuplicateCheckAndPersistence()
    {
        var repo = new Mock<IActionDataRepository>();
        repo.Setup(x => x.ExistsByNameAsync("Manage Users", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(x => x.AddAsync(It.IsAny<ActionEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var notify = new Mock<INotify>();
        var service = CreateService(repo, notify);
        var currentUser = Mock.Get(service.CurrentUser);
        currentUser.Setup(x => x.GetUserId()).Returns(7);

        var id = await service.App.CreateAsync(new CreateActionRequest { Name = "  Manage Users  ", Description = "Description" }, default);

        Assert.Equal(0, id);
        repo.Verify(x => x.ExistsByNameAsync("Manage Users", It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.AddAsync(It.Is<ActionEntity>(a => a.Name == "Manage Users"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DescriptionBoundary_255Accepted_256Rejected()
    {
        var localization = Localization();
        var validator = new CreateActionValidator(localization);
        var accepted = await validator.ValidateAsync(new ActionEntity("Name", new string('x', 255), 1));
        var rejected = await validator.ValidateAsync(new ActionEntity("Name", new string('x', 256), 1));

        Assert.True(accepted.IsValid);
        Assert.False(rejected.IsValid);
    }

    [Fact]
    public async Task BulkDuplicateNamesAreRejectedBeforePersistence()
    {
        var repo = new Mock<IActionDataRepository>();
        repo.Setup(x => x.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(x => x.AddManyAsync(It.IsAny<IReadOnlyCollection<ActionEntity>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var notify = new Mock<INotify>();
        var service = CreateService(repo, notify);
        var csv = "Name;Description\nCreate;First\n create ;Second\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var file = new FormFile(stream, 0, stream.Length, "file", "actions.csv");

        var result = await service.App.BulkUploadAsync(file, default);

        Assert.False(result);
        repo.Verify(x => x.AddManyAsync(It.IsAny<IReadOnlyCollection<ActionEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkTenThousandRowsAreAcceptedByTheBoundary()
    {
        var repo = new Mock<IActionDataRepository>();
        repo.Setup(x => x.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(x => x.AddManyAsync(It.IsAny<IReadOnlyCollection<ActionEntity>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var notify = new Mock<INotify>();
        var service = CreateService(repo, notify);
        var csv = new StringBuilder("Name;Description\n");
        for (var i = 0; i < 10_000; i++) csv.Append($"Action_{i};Description\n");
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));
        var file = new FormFile(stream, 0, stream.Length, "file", "actions.csv");

        Assert.True(await service.App.BulkUploadAsync(file, default));
        repo.Verify(x => x.AddManyAsync(It.Is<IReadOnlyCollection<ActionEntity>>(items => items.Count == 10_000), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkTenThousandAndOneRowsAreRejectedBeforePersistence()
    {
        var repo = new Mock<IActionDataRepository>();
        var notify = new Mock<INotify>();
        var service = CreateService(repo, notify);
        var csv = new StringBuilder("Name;Description\n");
        for (var i = 0; i < 10_001; i++) csv.Append($"Action_{i};Description\n");
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));
        var file = new FormFile(stream, 0, stream.Length, "file", "actions.csv");

        Assert.False(await service.App.BulkUploadAsync(file, default));
        repo.Verify(x => x.AddManyAsync(It.IsAny<IReadOnlyCollection<ActionEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (ActionAppService App, ICurrentUserService CurrentUser) CreateService(Mock<IActionDataRepository> repo, Mock<INotify> notify)
    {
        var currentUser = Mock.Of<ICurrentUserService>();
        var localization = Localization();
        var domain = new ActionDomainService(repo.Object, new ActionValidator(localization), notify.Object);
        var app = new ActionAppService(repo.Object, domain, notify.Object, currentUser, localization,
            NullLogger<ActionAppService>.Instance, Mock.Of<IFileValidationService>(x => x.ValidateFile(It.IsAny<IFormFile>())));
        return (app, currentUser);
    }

    private static ILocalizationService Localization()
    {
        var mock = new Mock<ILocalizationService>();
        mock.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        mock.Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object?[]>()))
            .Returns((string key, object?[] args) => key);
        return mock.Object;
    }
}
