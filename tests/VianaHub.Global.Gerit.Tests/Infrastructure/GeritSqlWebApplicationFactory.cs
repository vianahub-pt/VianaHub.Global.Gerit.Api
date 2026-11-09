using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VianaHub.Global.Gerit.Api.Helpers;

namespace VianaHub.Global.Gerit.Tests.Infrastructure;

public class GeritSqlWebApplicationFactory : WebApplicationFactory<VianaHub.Global.Gerit.Api.Program>
{
    protected virtual bool UseSimulatedAuthentication => true;
    private readonly string _encryptionKey = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    private readonly Dictionary<string, string?> _previousEnvironment = new();
    private readonly IServiceProvider? _previousServiceProvider;

    public GeritSqlWebApplicationFactory()
    {
        _previousServiceProvider = ServiceProviderHolder.ServiceProvider;
        var connectionString = Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        SetEnvironment("ConnectionStrings__DefaultConnection", connectionString);
        SetEnvironment("ConnectionStrings__HangfireConnection", connectionString);
        SetEnvironment("JwtKeyManagement__EncryptionKey", _encryptionKey);
        SetEnvironment("JwtSettings__Issuer", "roles-sql-tests");
        SetEnvironment("JwtSettings__Audience", "roles-sql-tests");
        SetEnvironment("JwtSettings__AccessTokenExpirationMinutes", "5");
        SetEnvironment("JwtSettings__RefreshTokenExpirationDays", "1");
        SetEnvironment("RateLimiting__EnableRateLimiting", "false");
        SetEnvironment("Cors__EnableCors", "false");
    }

    private void SetEnvironment(string name, string value)
    {
        _previousEnvironment[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("GERIT_RLS_TEST_CONNECTIONSTRING is required for SQL-backed HTTP tests.");

        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["ConnectionStrings:HangfireConnection"] = connectionString,
                ["JwtKeyManagement:EncryptionKey"] = _encryptionKey,
                ["JwtSettings:Issuer"] = "roles-sql-tests",
                ["JwtSettings:Audience"] = "roles-sql-tests",
                ["JwtSettings:AccessTokenExpirationMinutes"] = "5",
                ["JwtSettings:RefreshTokenExpirationDays"] = "1",
                ["RateLimiting:EnableRateLimiting"] = "false",
                ["Cors:EnableCors"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            if (UseSimulatedAuthentication)
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });

                services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                });
            }

            foreach (var descriptor in services.Where(x =>
                         x.ServiceType == typeof(IHostedService) &&
                         x.ImplementationType?.FullName?.Contains("Hangfire", StringComparison.OrdinalIgnoreCase) == true).ToList())
            {
                services.Remove(descriptor);
            }
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        try
        {
            return base.CreateHost(builder);
        }
        finally
        {
            RestoreEnvironment();
        }
    }

    private void RestoreEnvironment()
    {
        foreach (var pair in _previousEnvironment)
            Environment.SetEnvironmentVariable(pair.Key, pair.Value);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            RestoreEnvironment();
            ServiceProviderHolder.ServiceProvider = _previousServiceProvider;
        }

        base.Dispose(disposing);
    }
}
