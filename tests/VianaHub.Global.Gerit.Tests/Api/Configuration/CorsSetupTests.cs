using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VianaHub.Global.Gerit.Api.Configuration;
using Xunit;

namespace VianaHub.Global.Gerit.Tests.Api.Configuration
{
    public class CorsSetupTests
    {
        [Fact(DisplayName = "CORS - Quando desabilitado não lança exceção")]
        [Trait("Api", "")]
        public void AddCorsConfiguration_WhenEnableCorsFalse_DoesNotThrow()
        {
            var inMemory = new Dictionary<string, string?>
            {
                { "Cors:EnableCors", "false" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemory)
                .Build();

            var services = new ServiceCollection();

            var result = services.AddCorsConfiguration(configuration);

            Assert.Same(services, result);
        }

        [Fact(DisplayName = "CORS - AllowAnyOrigin em produção lança InvalidOperationException")]
        [Trait("Api", "")]
        public void AddCorsConfiguration_WithAllowAnyOriginInProduction_ThrowsInvalidOperationException()
        {
            var inMemory = new Dictionary<string, string?>
            {
                { "Cors:EnableCors", "true" },
                { "Cors:AllowedOrigins:0", "*" },
                { "ASPNETCORE_ENVIRONMENT", "Production" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemory)
                .Build();

            var services = new ServiceCollection();

            Assert.Throws<InvalidOperationException>(() => services.AddCorsConfiguration(configuration));
        }

        [Fact(DisplayName = "CORS - Quando origem específica adiciona policy")]
        [Trait("Api", "")]
        public void AddCorsConfiguration_WithSpecificOrigins_AddsPolicy()
        {
            var inMemory = new Dictionary<string, string?>
            {
                { "Cors:EnableCors", "true" },
                { "Cors:AllowedOrigins:0", "https://example.com" },
                { "Cors:AllowedMethods:0", "GET" },
                { "Cors:AllowedHeaders:0", "Content-Type" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemory)
                .Build();

            var services = new ServiceCollection();

            var result = services.AddCorsConfiguration(configuration);

            // Adiciona logging para resolver dependência de CorsService ao construir o provider
            services.AddLogging();

            // Resolve service provider para garantir que a policy foi registrada sem lançar
            var sp = services.BuildServiceProvider();
            var cors = sp.GetService<Microsoft.AspNetCore.Cors.Infrastructure.ICorsService>();

            Assert.NotNull(cors);
            Assert.Same(services, result);
        }
    }
}
