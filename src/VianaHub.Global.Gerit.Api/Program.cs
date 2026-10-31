using FluentValidation;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Http; // for RequestDelegateFactoryOptions
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;
using System.Text.Json.Serialization;
using VianaHub.Global.Gerit.Api.Configuration;
using VianaHub.Global.Gerit.Api.Configuration.Swagger;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Api.Middleware;
using VianaHub.Global.Gerit.Api.Security;
using VianaHub.Global.Gerit.Api.Services;
using VianaHub.Global.Gerit.Application.Mappings.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Interceptors;
using VianaHub.Global.Gerit.Infra.IoC;

namespace VianaHub.Global.Gerit.Api;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables()
                .Build())
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "VianaHub Gerit")
            .CreateLogger();

        var builder = WebApplication.CreateBuilder(args);

        ConfigurationValidator.ValidateConfiguration(builder.Configuration, builder.Environment);

        builder.Host.UseSerilog();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<TenantSessionConnectionInterceptor>();
        builder.Services.AddScoped<TenantSessionCommandInterceptor>();
        builder.Services.AddScoped<TelemetryInterceptor>();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwagger(builder.Configuration, builder.Environment);
        builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(ActionMappingProfile).Assembly));
        builder.Services.AddDbContext<GeritDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

            var tenantConnectionInterceptor = serviceProvider.GetRequiredService<TenantSessionConnectionInterceptor>();
            var tenantCommandInterceptor = serviceProvider.GetRequiredService<TenantSessionCommandInterceptor>();
            var telemetryInterceptor = serviceProvider.GetRequiredService<TelemetryInterceptor>();

            options.AddInterceptors(
                tenantConnectionInterceptor,
                tenantCommandInterceptor,
                telemetryInterceptor
            );
        });
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
        builder.Services.AddJwt(builder.Configuration);
        builder.Services.AddRouteValidatorSetup();
        // Add authorization and register named policies
        builder.Services.AddAuthorization(options =>
        {
            // 'BackOffice' policy used by endpoints. Requires authenticated user; adjust requirements as needed.
            options.AddPolicy("BackOffice", policy =>
            {
                policy.RequireAuthenticatedUser();
                // Additional requirements (roles/claims) can be added here, e.g.:
                // policy.RequireRole("Admin");
            });
        });
        builder.Services.AddRateLimitingConfiguration(builder.Configuration);
        builder.Services.AddCorsConfiguration(builder.Configuration);

        // Configuração do dashboard Hangfire (Basic Auth)
        builder.Services.Configure<HangfireDashboardSettings>(
            builder.Configuration.GetSection("HangfireDashboard"));
        builder.Services.AddTransient<HangfireDashboardAuthorizationFilter>();

        // Hangfire configuration - usa conex?o separada se fornecida (HangfireConnection)
        var hangfireConn = builder.Configuration.GetConnectionString("HangfireConnection")
                          ?? builder.Configuration.GetConnectionString("DefaultConnection");

        builder.Services.AddHangfire(configuration =>
        {
            configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                         .UseSimpleAssemblyNameTypeSerializer()
                         .UseRecommendedSerializerSettings()
                         .UseSqlServerStorage(hangfireConn, new SqlServerStorageOptions
                         {
                             SchemaName = "dbo",
                             PrepareSchemaIfNecessary = true,
                             QueuePollInterval = TimeSpan.FromSeconds(15)
                         });
        });

        builder.Services.AddHangfireServer();

        // Registrar infra services - chama o método que registra todos os serviços da aplicação
        builder.Services.AddGeritInfrastructure();

        // Localization service can be resolved from root during app startup (health endpoint)
        // and is thread-safe (uses internal cache), so register as singleton to avoid
        // 'Cannot resolve scoped service from root provider' errors.
        builder.Services.AddScoped<ICurrentUserService, CurrentUserApiService>();
        builder.Services.AddSingleton<ILocalizationService, LocalizationService>();

        var app = builder.Build();

        // Inicializar ServiceProviderHolder para uso em helpers estáticos
        ServiceProviderHolder.ServiceProvider = app.Services;

        // Habilita arquivos estáticos (custom.css e custom.js do Swagger)
        app.UseStaticFiles();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
        {
            app.UseSwaggerConfiguration();
        }

        // Register RequestLocalizationMiddleware early so RequestCulture is always set
        app.UseMiddleware<RequestLocalizationMiddleware>();

        // IMPORTANTE: Middleware de tratamento global de exceções DEVE vir antes de outros middlewares
        // para capturar qualquer exceção não tratada
        app.UseMiddleware<GlobalExceptionMiddleware>();

        app.UseHttpsRedirection();

        // Rate Limiting (apenas se habilitado)
        var rateLimitingEnabled = builder.Configuration.GetValue<bool>("RateLimiting:EnableRateLimiting", true);
        if (rateLimitingEnabled)
        {
            app.UseRateLimiter();
        }

        // CORS (apenas se habilitado)
        var corsEnabled = builder.Configuration.GetValue<bool>("Cors:EnableCors", true);
        if (corsEnabled)
        {
            var policyName = builder.Configuration.GetValue<string>("Cors:PolicyName") ?? "GeritCorsPolicy";
            app.UseCors(policyName);
        }

        app.UseAuthentication();
        app.UseAuthorization();

        // Hangfire dashboard (protegido por Basic Auth - configurável via appsettings Hangfire)
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { app.Services.GetRequiredService<HangfireDashboardAuthorizationFilter>() }
        });

        // Registro automático de todos os endpoints via reflexão
        app.MapEndpointsFromAssembly();

        // Minimal API Endpoints
        var localization = app.Services.GetService(typeof(ILocalizationService)) as ILocalizationService;

        app.MapGet("/health", () => Results.Ok(new
        {
            Status = localization?.GetMessage("Api.Health.Status") ?? "Api.Health.Status",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        }))
        .WithName("HealthCheck")
        .WithTags("Health")
        .Produces(200)
        .WithSummary(localization?.GetMessage("Api.Health.Summary") ?? "Api.Health.Summary")
        .WithDescription(localization?.GetMessage("Api.Health.Description") ?? "Api.Health.Description");

        app.Run();
    }
}