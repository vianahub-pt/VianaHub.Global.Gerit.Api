using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using VianaHub.Global.Gerit.Api.Configuration;
using Xunit;

namespace VianaHub.Global.Gerit.Tests.Api.Configuration
{
    public class SerilogConfigurationTests
    {
        [Fact(DisplayName = "Serilog - Configurar não lança exceção")]
        [Trait("Api", "")]
        public void ConfigureSerilog_DoesNotThrow()
        {
            var inMemory = new Dictionary<string, string?>
            {
                { "Logging:LogLevel:Default", "Information" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemory)
                .Build();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = new string[0]
            });

            builder.Configuration.AddConfiguration(configuration);

            // Should not throw
            builder.ConfigureSerilog();
        }

        [Fact(DisplayName = "Serilog - Usar logging de requisição não lança exceção")]
        [Trait("Api", "")]
        public void UseSerilogRequestLogging_AddsMiddleware()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = new string[0]
            });

            var app = builder.Build();

            // Should not throw
            app.UseSerilogRequestLogging();
        }
    }
}
