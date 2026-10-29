using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Identity;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Services.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Validators.Identity.Action;

namespace VianaHub.Global.Gerit.Tests;

public class IdempotentActionTests
{
    private static ILocalizationService Localization
    {
        get
        {
            var mock = new Mock<ILocalizationService>();
            mock.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
            mock.Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object?[]>()))
                .Returns((string key, object?[] args) => key);
            return mock.Object;
        }
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Action_StateChangeAndRepeat_ShouldSucceed(bool activate, bool initiallyActive)
    {
        var entity = new ActionEntity("Read", "Read", 1);
        SetId(entity, 1);
        if (!initiallyActive) entity.Deactivate(1);
        var repo = new Mock<IActionDataRepository>();
        repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repo.Setup(x => x.UpdateAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var notify = new Mock<INotify>();
        var localization = Localization;
        var domain = new ActionDomainService(repo.Object, new ActionValidator(localization), notify.Object);
        var app = new ActionAppService(repo.Object, domain, notify.Object, Mock.Of<IMapper>(),
            Mock.Of<ICurrentUserService>(), localization, NullLogger<ActionAppService>.Instance,
            Mock.Of<IFileValidationService>());

        for (var i = 0; i < 2; i++)
        {
            Assert.True(activate ? await app.ActivateAsync(1, default) : await app.DeactivateAsync(1, default));
            Assert.Equal(activate, entity.IsActive);
        }
        notify.Verify(x => x.Add(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        repo.Verify(x => x.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task Action_Validators_ShouldPreserveIdAndDeletedRules(int id, bool deleted)
    {
        var entity = new ActionEntity("Read", "Read", 1);
        SetId(entity, id);
        if (deleted) entity.Delete(1);
        var validator = new ActionValidator(Localization);

        Assert.False((await validator.ValidateForActivateAsync(entity)).IsValid);
        Assert.False((await validator.ValidateForDeactivateAsync(entity)).IsValid);
    }

    [Fact]
    public async Task Action_Delete_ShouldSucceedWithoutAlreadyDeletedValidation()
    {
        var entity = new ActionEntity("Read", "Read", 1);
        SetId(entity, 1);
        var repo = new Mock<IActionDataRepository>();
        repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repo.Setup(x => x.UpdateAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var notify = new Mock<INotify>();
        var localization = Localization;
        var domain = new ActionDomainService(repo.Object, new ActionValidator(localization), notify.Object);
        var app = new ActionAppService(repo.Object, domain, notify.Object, Mock.Of<IMapper>(),
            Mock.Of<ICurrentUserService>(), localization, NullLogger<ActionAppService>.Instance,
            Mock.Of<IFileValidationService>());

        Assert.True(await app.DeleteAsync(1, default));
        Assert.True(entity.IsDeleted);
        Assert.False(entity.IsActive);
        notify.Verify(x => x.Add(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        repo.Verify(x => x.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Action_DeleteValidator_ShouldAcceptEitherDeletedState(bool deleted)
    {
        var entity = new ActionEntity("Read", "Read", 1);
        SetId(entity, 1);
        if (deleted) entity.Delete(1);

        Assert.True((await new ActionValidator(Localization).ValidateForDeleteAsync(entity)).IsValid);
    }

    [Fact]
    public async Task Action_DeleteValidator_ShouldPreserveIdRule()
    {
        var entity = new ActionEntity("Read", "Read", 1);
        entity.Delete(1);

        var result = await new ActionValidator(Localization).ValidateForDeleteAsync(entity);

        Assert.Contains(result.Errors, x => x.ErrorMessage == "Domain.Action.IdRequired");
    }

    [Theory]
    [InlineData("Activate", false)]
    [InlineData("Activate", true)]
    [InlineData("Deactivate", false)]
    [InlineData("Deactivate", true)]
    [InlineData("Delete", false)]
    [InlineData("Delete", true)]
    public async Task Action_ShouldPreserveGoneRuleForMissingOrDeletedEntity(string operation, bool deleted)
    {
        ActionEntity? entity = null;
        if (deleted)
        {
            entity = new ActionEntity("Read", "Read", 1);
            SetId(entity, 1);
            entity.Delete(1);
        }
        var repo = new Mock<IActionDataRepository>();
        repo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var notify = new Mock<INotify>();
        var localization = Localization;
        var domain = new ActionDomainService(repo.Object, new ActionValidator(localization), notify.Object);
        var app = new ActionAppService(repo.Object, domain, notify.Object, Mock.Of<IMapper>(),
            Mock.Of<ICurrentUserService>(), localization, NullLogger<ActionAppService>.Instance,
            Mock.Of<IFileValidationService>());

        var result = operation switch
        {
            "Activate" => await app.ActivateAsync(1, default),
            "Deactivate" => await app.DeactivateAsync(1, default),
            _ => await app.DeleteAsync(1, default)
        };

        Assert.False(result);
        notify.Verify(x => x.Add($"Application.Service.Action.{operation}.ResourceNotFound", 410), Times.Once);
        repo.Verify(x => x.UpdateAsync(It.IsAny<ActionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static void SetId(Entity entity, int id) => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);
}
