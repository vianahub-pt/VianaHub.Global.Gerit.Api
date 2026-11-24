using FluentValidation.Results;
using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Request.Job;
using VianaHub.Global.Gerit.Application.Interfaces.Job;
using VianaHub.Global.Gerit.Application.Services.Job;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Job;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Job;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Tests.Job;

public sealed class JobAppServiceUpdateSchedulerOrderingTests
{
    [Fact]
    public async Task UpdateAsync_DoesNotTouchSchedulerWhenPersistenceFails()
    {
        var fixture = new Fixture();
        fixture.Repository.Setup(x => x.UpdateAsync(fixture.Job, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Assert.False(await fixture.Service.UpdateAsync(42, Request(isActive: true), default));

        fixture.Scheduler.Verify(x => x.RegisterRecurringAsync(It.IsAny<JobDefinitionsEntity>()), Times.Never);
        fixture.Scheduler.Verify(x => x.RemoveRecurringAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_PersistsBeforeRegisteringRecurringJob()
    {
        var fixture = new Fixture();
        var calls = new List<string>();
        fixture.Repository.Setup(x => x.UpdateAsync(fixture.Job, It.IsAny<CancellationToken>()))
            .Callback<JobDefinitionsEntity, CancellationToken>((entity, _) =>
            {
                Assert.Equal("QuarterlyReport", entity.HangfireJobId);
                calls.Add("persist");
            })
            .ReturnsAsync(true);
        fixture.Scheduler.Setup(x => x.RegisterRecurringAsync(fixture.Job))
            .Callback(() => calls.Add("register"))
            .Returns(Task.CompletedTask);

        Assert.True(await fixture.Service.UpdateAsync(42, Request(isActive: true), default));

        Assert.Equal(["persist", "register"], calls);
        fixture.Repository.Verify(x => x.UpdateAsync(fixture.Job, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_PersistsBeforeRemovingRecurringJob()
    {
        var fixture = new Fixture();
        var calls = new List<string>();
        fixture.Repository.Setup(x => x.UpdateAsync(fixture.Job, It.IsAny<CancellationToken>()))
            .Callback<JobDefinitionsEntity, CancellationToken>((_, _) => calls.Add("persist"))
            .ReturnsAsync(true);
        fixture.Scheduler.Setup(x => x.RemoveRecurringAsync("QuarterlyReport"))
            .Callback(() => calls.Add("remove"))
            .Returns(Task.CompletedTask);

        Assert.True(await fixture.Service.UpdateAsync(42, Request(isActive: false), default));

        Assert.Equal(["persist", "remove"], calls);
        fixture.Scheduler.Verify(x => x.RemoveRecurringAsync("QuarterlyReport"), Times.Once);
        fixture.Scheduler.Verify(x => x.RegisterRecurringAsync(It.IsAny<JobDefinitionsEntity>()), Times.Never);
    }

    private static UpdateJobRequest Request(bool? isActive) => new()
    {
        Description = "Updated description",
        JobPurpose = "Report",
        CronExpression = "0 0 * * *",
        TimeZoneId = "UTC",
        TimeoutMinutes = 10,
        Priority = 5,
        Queue = "default",
        MaxRetries = 3,
        JobConfiguration = "{}",
        IsActive = isActive
    };

    private sealed class Fixture
    {
        public Mock<IJobDefinitionDataRepository> Repository { get; } = new();
        public Mock<IEntityDomainValidator<JobDefinitionsEntity>> Validator { get; } = new();
        public Mock<IJobSchedulerService> Scheduler { get; } = new();
        public JobDefinitionsEntity Job { get; } = new(
            "Operations",
            "QuarterlyReport",
            "Recurring",
            1,
            description: "Original",
            cronExpression: "0 0 * * *",
            timeZoneId: "UTC",
            executeOnlyOnce: false);
        public JobAppService Service { get; }

        public Fixture()
        {
            Job.SetHangfireRegistration("QuarterlyReport");
            Repository.Setup(x => x.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(Job);
            Validator.Setup(x => x.ValidateForUpdateAsync(Job)).ReturnsAsync(new ValidationResult());
            var localization = new Mock<ILocalizationService>();
            localization.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(x => x.GetUserId()).Returns(17);
            Service = new JobAppService(Repository.Object, new Notify(), localization.Object, currentUser.Object, Validator.Object, Scheduler.Object);
        }
    }
}
