using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VianaHub.Global.Gerit.Api.Configuration.Swagger;
using Moq;
using Xunit;

namespace VianaHub.Global.Gerit.Tests.Api.Configuration
{
    public class SwaggerSetupTests
    {
        [Fact(DisplayName = "Swagger - Adicionar não lança exceção")]
        [Trait("Api", "")]
        public void AddSwagger_DoesNotThrow()
        {
            var inMemory = new Dictionary<string, string?>
            {
                { "Swagger:Title", "Test" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemory)
                .Build();

            var services = new ServiceCollection();

            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.EnvironmentName).Returns("Development");

            var result = services.AddSwagger(configuration, envMock.Object);

            Assert.Same(services, result);
        }
    }
}
