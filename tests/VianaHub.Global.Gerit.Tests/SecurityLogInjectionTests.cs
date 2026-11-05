using System.Security.Claims;
using System.Text;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VianaHub.Global.Gerit.Api.Configuration;
using VianaHub.Global.Gerit.Api.Filters;
using VianaHub.Global.Gerit.Api.Security;
using VianaHub.Global.Gerit.Domain.Entities.Job;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Job;
using VianaHub.Global.Gerit.Infra.Integration.Messaging;
using VianaHub.Global.Gerit.Infra.Job.Services;
using AutoMapper;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Tests;

public class SecurityLogInjectionTests
{
    private const string Attack = "value\r\nFORGED\n\u0085\u2028\u2029";

    [Theory]
    [InlineData("\r", "\\r")]
    [InlineData("\n", "\\n")]
    [InlineData("\r\n", "\\r\\n")]
    [InlineData("\u0085", "\\u0085")]
    [InlineData("\u2028", "\\u2028")]
    [InlineData("\u2029", "\\u2029")]
    [InlineData("ação", "ação")]
    [InlineData(null, null)]
    public void Sanitizer_EncodesLineSeparatorsAndPreservesOtherValues(string? value, string? expected)
    {
        Assert.Equal(expected, LogValueSanitizer.Sanitize(value));
    }

    [Fact]
    public async Task EmailSender_EncodesRecipientAndLocalizedContent()
    {
        var logger = new RecordingLogger<NoOpEmailSender>();
        var localization = new Mock<ILocalizationService>();
        localization.Setup(l => l.GetMessage("subject")).Returns(Attack);
        localization.Setup(l => l.GetMessage("body", It.IsAny<object?[]>())).Returns(Attack);

        await new NoOpEmailSender(logger, localization.Object).SendAsync(Attack, "subject", "body", Attack);

        Assert.Equal(2, logger.Messages.Count);
        AssertSafe(logger.Messages);
        Assert.Contains(logger.Messages, m => m.Contains("\\r\\nFORGED", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CsvFailure_EncodesExceptionPayloadAndKeepsFailureNotification()
    {
        var logger = new RecordingLogger<ActionAppService>();
        var validation = new Mock<IFileValidationService>();
        validation.Setup(v => v.ValidateFile(It.IsAny<IFormFile>())).Returns(true);
        var file = new Mock<IFormFile>();
        file.Setup(f => f.OpenReadStream()).Throws(new InvalidDataException(Attack));
        var localization = new Mock<ILocalizationService>();
        localization.Setup(l => l.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        var notify = new Notify();
        var service = new ActionAppService(Mock.Of<IActionDataRepository>(), Mock.Of<IActionDomainService>(),
            notify, Mock.Of<IMapper>(), Mock.Of<ICurrentUserService>(), localization.Object, logger, validation.Object);

        Assert.False(await service.BulkUploadAsync(file.Object, CancellationToken.None));
        Assert.True(notify.HasNotify());
        AssertSafe(logger.Messages);
        Assert.All(logger.Exceptions, exception => Assert.Null(exception));
        Assert.Contains(logger.Messages, m => m.Contains("InvalidDataException", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Scheduler_EncodesJobNamesAndExceptionDetailsWithoutChangingExecutionInput()
    {
        var logger = new RecordingLogger<HangfireJobService>();
        var executor = new Mock<IJobExecutor>();
        executor.Setup(e => e.RegisterRecurringJobAsync(It.IsAny<JobDefinitionsEntity>())).ReturnsAsync(false);
        executor.Setup(e => e.RemoveRecurringJobAsync(Attack)).ReturnsAsync(true);
        using var services = new ServiceCollection().BuildServiceProvider();
        var scheduler = new HangfireJobService(services, logger, executor.Object);
        var job = new JobDefinitionsEntity("test", Attack, "test", 1,
            cronExpression: "* * * * *", timeZoneId: Attack);

        await scheduler.RegisterRecurringAsync(job);
        await scheduler.RemoveRecurringAsync(Attack);

        executor.Verify(e => e.RegisterRecurringJobAsync(job), Times.Once);
        executor.Verify(e => e.RemoveRecurringJobAsync(Attack), Times.Once);
        Assert.Equal(Attack, job.JobName);
        AssertSafe(logger.Messages);
        Assert.All(logger.Exceptions, exception => Assert.Null(exception));
        Assert.Contains(logger.Messages, m => m.Contains("Details: ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UserValidation_RejectsAndSafelyLogsMalformedRouteInput()
    {
        var logger = new RecordingLogger<UserValidationFilter>();
        using var services = new ServiceCollection().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.RouteValues["userId"] = Attack;
        var nextCalled = false;

        var result = await new UserValidationFilter(logger).InvokeAsync(
            new DefaultEndpointFilterInvocationContext(context), _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(null);
            });

        Assert.NotNull(result);
        Assert.False(nextCalled);
        AssertSafe(logger.Messages);
        Assert.Contains(logger.Messages, m => m.Contains("\\r\\nFORGED", StringComparison.Ordinal));
    }

    [Fact]
    public async Task JwtCallbacks_EncodeRequestPathsClaimsAndErrors()
    {
        var logger = new RecordingLogger<JwtBearerHandler>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<ILogger<JwtBearerHandler>>(logger);
        serviceCollection.AddJwt(new ConfigurationBuilder().Build());
        using var services = serviceCollection.BuildServiceProvider();
        var options = services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        var scheme = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Path = "/" + Attack;

        await options.Events.AuthenticationFailed(new AuthenticationFailedContext(context, scheme, options)
        {
            Exception = new InvalidOperationException(Attack)
        });
        await options.Events.TokenValidated(new TokenValidatedContext(context, scheme, options)
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Attack)], "test"))
        });
        await options.Events.Challenge(new JwtBearerChallengeContext(context, scheme, options, new AuthenticationProperties()) { Error = Attack });

        Assert.Equal(3, logger.Messages.Count);
        AssertSafe(logger.Messages);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Bearer invalid")]
    [InlineData("Basic ###")]
    public void Dashboard_UntrustedHeadersCannotBypassAuthentication(string? authorization)
    {
        var (filter, dashboard, _, context) = CreateDashboard();
        if (authorization != null) context.Request.Headers.Authorization = authorization;

        Assert.False(filter.Authorize(dashboard));
        Assert.Contains("Basic realm=", context.Response.Headers.WWWAuthenticate.ToString());
    }

    [Fact]
    public void Dashboard_DeniesWrongCredentialsAndEncodesUsername()
    {
        var (filter, dashboard, logger, context) = CreateDashboard();
        context.Request.Headers.Authorization = Basic(Attack, "unit-test-only");

        Assert.False(filter.Authorize(dashboard));
        AssertSafe(logger.Messages);
        Assert.Contains(logger.Messages, m => m.Contains("\\r\\nFORGED", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Dashboard_MissingConfiguredPasswordFailsClosed(string? password)
    {
        var (filter, dashboard, _, context) = CreateDashboard(password);
        context.Request.Headers.Authorization = Basic("operator", password ?? "");

        Assert.False(filter.Authorize(dashboard));
    }

    [Fact]
    public void Dashboard_GrantsAccessOnlyAfterCredentialComparison()
    {
        var (filter, dashboard, logger, context) = CreateDashboard();
        context.Request.Headers.Authorization = Basic("operator", "unit-test-only");

        Assert.True(filter.Authorize(dashboard));
        AssertSafe(logger.Messages);
    }

    private static string Basic(string user, string password) =>
        "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(user + ":" + password));

    private static (HangfireDashboardAuthorizationFilter Filter, DashboardContext Dashboard,
        RecordingLogger<HangfireDashboardAuthorizationFilter> Logger, DefaultHttpContext Context)
        CreateDashboard(string? password = "unit-test-only")
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        var logger = new RecordingLogger<HangfireDashboardAuthorizationFilter>();
        var options = Options.Create(new HangfireDashboardSettings { Username = "operator", Password = password });
        var filter = new HangfireDashboardAuthorizationFilter(options, logger);
        var dashboard = new AspNetCoreDashboardContext(Mock.Of<JobStorage>(), new DashboardOptions(), context);
        return (filter, dashboard, logger, context);
    }

    private static void AssertSafe(IEnumerable<string> messages)
    {
        Assert.NotEmpty(messages);
        foreach (var message in messages)
        {
            Assert.DoesNotContain('\r', message);
            Assert.DoesNotContain('\n', message);
            Assert.DoesNotContain('\u0085', message);
            Assert.DoesNotContain('\u2028', message);
            Assert.DoesNotContain('\u2029', message);
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public List<Exception?> Exceptions { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            Exceptions.Add(exception);
        }
    }
}
