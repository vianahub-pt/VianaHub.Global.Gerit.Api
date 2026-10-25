using VianaHub.Global.Gerit.Api.Endpoints.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using VianaHub.Global.Gerit.Api.Endpoints.Identity;
using Moq;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Api.Endpoints.Billing;
using VianaHub.Global.Gerit.Application.Interfaces.Billing;
using VianaHub.Global.Gerit.Application.Interfaces.Job;
using VianaHub.Global.Gerit.Api.Endpoints.Job;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using Xunit;

namespace VianaHub.Global.Gerit.Tests.Api.Endpoints
{
    public class EndpointsRegistrationTests
    {
        private sealed class TestEndpointRouteBuilder : IEndpointRouteBuilder
        {
            public TestEndpointRouteBuilder(IServiceProvider serviceProvider)
            {
                ServiceProvider = serviceProvider;
                DataSources = new List<EndpointDataSource>();
            }

            public IServiceProvider ServiceProvider { get; }
            public ICollection<EndpointDataSource> DataSources { get; }

            public IApplicationBuilder CreateApplicationBuilder()
            {
                return new ApplicationBuilder(ServiceProvider);
            }
        }

        private static IEndpointRouteBuilder CreateRouteBuilder()
        {
            var services = new ServiceCollection();
            services.AddRouting();
            services.AddLogging();

            // register mocks for app services and other dependencies used by endpoints
            services.AddSingleton(new Mock<INotify>().Object);
            services.AddSingleton(new Mock<IPlanAppService>().Object);
            services.AddSingleton(new Mock<ISubscriptionAppService>().Object);
            services.AddSingleton(new Mock<ITenantAppService>().Object);
            services.AddSingleton(new Mock<IRoleAppService>().Object);
            services.AddSingleton(new Mock<IResourceAppService>().Object);
            services.AddSingleton(new Mock<IActionAppService>().Object);
            services.AddSingleton(new Mock<IRolePermissionAppService>().Object);
            services.AddSingleton(new Mock<IUserRoleAppService>().Object);
            services.AddSingleton(new Mock<IJobAppService>().Object);
            services.AddSingleton(new Mock<IJwtKeyAppService>().Object);
            services.AddSingleton(new Mock<IAuthAppService>().Object);
            services.AddSingleton(new Mock<ITenantContactPersonAppService>().Object);
            services.AddSingleton(new Mock<ILocalizationService>().Object);
            services.AddSingleton(new Mock<IAddressTypeAppService>().Object);
            services.AddSingleton(new Mock<IAcquisitionSourceTypeAppService>().Object);
            services.AddSingleton(new Mock<IDocumentTypeAppService>().Object);
            services.AddSingleton(new Mock<IPartyTypeAppService>().Object);
            services.AddSingleton(new Mock<IStatusDomainAppService>().Object);
            services.AddSingleton(new Mock<IStatusDefinitionAppService>().Object);

            var sp = services.BuildServiceProvider();
            return new TestEndpointRouteBuilder(sp);
        }

        private static Microsoft.AspNetCore.Builder.WebApplication CreateWebApplication()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddLogging();
            builder.Services.AddRouting();

            builder.Services.AddSingleton(new Mock<INotify>().Object);
            builder.Services.AddSingleton(new Mock<IPlanAppService>().Object);
            builder.Services.AddSingleton(new Mock<ISubscriptionAppService>().Object);
            builder.Services.AddSingleton(new Mock<ITenantAppService>().Object);
            builder.Services.AddSingleton(new Mock<IRoleAppService>().Object);
            builder.Services.AddSingleton(new Mock<IResourceAppService>().Object);
            builder.Services.AddSingleton(new Mock<IActionAppService>().Object);
            builder.Services.AddSingleton(new Mock<IRolePermissionAppService>().Object);
            builder.Services.AddSingleton(new Mock<IUserRoleAppService>().Object);
            builder.Services.AddSingleton(new Mock<IJobAppService>().Object);
            builder.Services.AddSingleton(new Mock<IJwtKeyAppService>().Object);
            builder.Services.AddSingleton(new Mock<IAuthAppService>().Object);
            builder.Services.AddSingleton(new Mock<ITenantContactPersonAppService>().Object);
            builder.Services.AddSingleton(new Mock<ILocalizationService>().Object);

            var app = builder.Build();
            return app;
        }

        [Theory]
        [InlineData("CreateAddressType", typeof(VianaHub.Global.Gerit.Application.Dtos.Request.Business.AddressType.CreateAddressTypeRequest))]
        [InlineData("CreateAcquisitionSourceType", typeof(VianaHub.Global.Gerit.Application.Dtos.Request.Business.AcquisitionSourceType.CreateAcquisitionSourceTypeRequest))]
        [InlineData("CreateDocumentType", typeof(VianaHub.Global.Gerit.Application.Dtos.Request.Business.DocumentType.CreateDocumentTypeRequest))]
        [InlineData("CreatePartyType", typeof(VianaHub.Global.Gerit.Application.Dtos.Request.Business.PartyType.CreatePartyTypeRequest))]
        [InlineData("CreateStatusDomain", typeof(VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDomain.CreateStatusDomainRequest))]
        [InlineData("CreateStatusDefinition", typeof(VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDefinition.CreateStatusDefinitionRequest))]
        [InlineData("CreatePlan", typeof(VianaHub.Global.Gerit.Application.Dtos.Request.Billing.Plan.CreatePlanRequest))]
        public void CanonicalCatalogCreates_ExposeTranslationRequestAndActualResponses(string endpointName, Type requestType)
        {
            var routes = CreateRouteBuilder();
            switch (endpointName)
            {
                case "CreateAddressType": routes.MapAddressTypeEndpoints(); break;
                case "CreateAcquisitionSourceType": routes.MapAcquisitionSourceTypeEndpoints(); break;
                case "CreateDocumentType": routes.MapDocumentTypeEndpoints(); break;
                case "CreatePartyType": routes.MapPartyTypeEndpoints(); break;
                case "CreateStatusDomain": routes.MapStatusDomainEndpoints(); break;
                case "CreateStatusDefinition": routes.MapStatusDefinitionEndpoints(); break;
                case "CreatePlan": routes.MapPlanEndpoints(); break;
            }
            var endpoint = routes.DataSources.SelectMany(source => source.Endpoints)
                .Single(item => item.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == endpointName);
            var accepts = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Http.Metadata.IAcceptsMetadata>();
            Assert.NotNull(accepts);
            Assert.Equal(requestType, accepts.RequestType);
            Assert.Contains("application/json", accepts.ContentTypes);
            Assert.NotNull(requestType.GetProperty("Translations"));
            Assert.True(Attribute.IsDefined(requestType.GetProperty("Translations")!, typeof(System.ComponentModel.DataAnnotations.RequiredAttribute)));
            Assert.Null(requestType.GetProperty("Code"));
            Assert.Null(requestType.GetProperty("Name"));
            var responses = endpoint.Metadata.GetOrderedMetadata<Microsoft.AspNetCore.Http.Metadata.IProducesResponseTypeMetadata>();
            Assert.Contains(responses, response => response.StatusCode == 201 && response.Type == typeof(VianaHub.Global.Gerit.Application.Dtos.Base.GenericResponse));
            Assert.Contains(responses, response => response.StatusCode == 400 && response.Type == typeof(VianaHub.Global.Gerit.Application.Dtos.Base.ErrorResponse));
            Assert.Contains(responses, response => response.StatusCode == 409 && response.Type == typeof(VianaHub.Global.Gerit.Application.Dtos.Base.ErrorResponse));
        }

        [Theory]
        [InlineData(typeof(VianaHub.Global.Gerit.Application.Dtos.Request.Business.AddressType.CreateAddressTypeRequest))]
        [InlineData(typeof(VianaHub.Global.Gerit.Application.Dtos.Request.Business.AcquisitionSourceType.CreateAcquisitionSourceTypeRequest))]
        [InlineData(typeof(VianaHub.Global.Gerit.Application.Dtos.Request.Business.DocumentType.CreateDocumentTypeRequest))]
        [InlineData(typeof(VianaHub.Global.Gerit.Application.Dtos.Request.Business.PartyType.CreatePartyTypeRequest))]
        [InlineData(typeof(VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDomain.CreateStatusDomainRequest))]
        [InlineData(typeof(VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDefinition.CreateStatusDefinitionRequest))]
        [InlineData(typeof(VianaHub.Global.Gerit.Application.Dtos.Request.Billing.Plan.CreatePlanRequest))]
        public async Task CanonicalCatalogCreateValidators_AreRegisteredAndRejectMissingEnglish(Type requestType)
        {
            var localization = new Mock<ILocalizationService>();
            localization.Setup(service => service.GetMessage(It.IsAny<string>())).Returns((string key) => key);
            localization.Setup(service => service.GetMessage(It.IsAny<string>(), It.IsAny<object[]>())).Returns((string key, object[] arguments) => key);
            var services = new ServiceCollection();
            services.AddSingleton(localization.Object);
            VianaHub.Global.Gerit.Api.Configuration.RouteValidatorSetup.AddRouteValidatorSetup(services);
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var validator = (FluentValidation.IValidator)scope.ServiceProvider.GetRequiredService(typeof(FluentValidation.IValidator<>).MakeGenericType(requestType));
            var request = Activator.CreateInstance(requestType)!;
            var result = await validator.ValidateAsync(new FluentValidation.ValidationContext<object>(request));
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, failure => failure.ErrorMessage == "Application.Catalog.CanonicalEnglishNameRequired");
        }

        [Theory]
        [InlineData("GetAllActions", typeof(IEnumerable<VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Action.ActionResponse>))]
        [InlineData("GetActionById", typeof(VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Action.ActionDetailResponse))]
        public void ActionEndpoints_DeclareActualSuccessBody(string endpointName, Type responseType)
        {
            var routes = CreateRouteBuilder();
            ActionEndpoint.MapActionEndpoints(routes);
            var endpoint = routes.DataSources.SelectMany(source => source.Endpoints)
                .Single(item => item.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == endpointName);
            var response = endpoint.Metadata.GetOrderedMetadata<Microsoft.AspNetCore.Http.Metadata.IProducesResponseTypeMetadata>()
                .Last(item => item.StatusCode == 200);
            Assert.Equal(responseType, response.Type);
        }

        [Fact(DisplayName = "Endpoints - MapPlanEndpoints n�o lan�a exce��o")]
        [Trait("Api", "")]
        public void MapPlanEndpoints_DoesNotThrow()
        {
            var routeBuilder = CreateRouteBuilder();

            var ex = Record.Exception(() => PlanEndpoint.MapPlanEndpoints(routeBuilder));

            Assert.Null(ex);
        }

        [Fact(DisplayName = "Endpoints - MapSubscriptionEndpoints n�o lan�a exce��o")]
        [Trait("Api", "")]
        public void MapSubscriptionEndpoints_DoesNotThrow()
        {
            var routeBuilder = CreateRouteBuilder();
            var ex = Record.Exception(() => SubscriptionEndpoint.MapSubscriptionEndpoints(routeBuilder));
            Assert.Null(ex);
        }

        [Fact(DisplayName = "Endpoints - MapTenantEndpoints n�o lan�a exce��o")]
        [Trait("Api", "")]
        public void MapTenantEndpoints_DoesNotThrow()
        {
            var routeBuilder = CreateRouteBuilder();
            var ex = Record.Exception(() => TenantEndpoint.MapTenantEndpoints(routeBuilder));
            Assert.Null(ex);
        }

        [Fact(DisplayName = "Endpoints - MapRoleEndpoints n�o lan�a exce��o")]
        [Trait("Api", "")]
        public void MapRoleEndpoints_DoesNotThrow()
        {
            var routeBuilder = CreateRouteBuilder();
            var ex = Record.Exception(() => RoleEndpoint.MapRoleEndpoints(routeBuilder));
            Assert.Null(ex);
        }

        [Fact(DisplayName = "Endpoints - MapResourceEndpoints n�o lan�a exce��o")]
        [Trait("Api", "")]
        public void MapResourceEndpoints_DoesNotThrow()
        {
            var routeBuilder = CreateRouteBuilder();
            var ex = Record.Exception(() => ResourceEndpoint.MapResourceEndpoints(routeBuilder));
            Assert.Null(ex);
        }

        [Fact(DisplayName = "Endpoints - MapActionEndpoints n�o lan�a exce��o")]
        [Trait("Api", "")]
        public void MapActionEndpoints_DoesNotThrow()
        {
            var routeBuilder = CreateRouteBuilder();
            var ex = Record.Exception(() => ActionEndpoint.MapActionEndpoints(routeBuilder));
            Assert.Null(ex);
        }

        [Fact(DisplayName = "Endpoints - MapRolePermissionEndpoints n�o lan�a exce��o")]
        [Trait("Api", "")]
        public void MapRolePermissionEndpoints_DoesNotThrow()
        {
            var routeBuilder = CreateRouteBuilder();
            var ex = Record.Exception(() => RolePermissionEndpoint.MapRolePermissionEndpoints(routeBuilder));
            Assert.Null(ex);
        }

        [Fact(DisplayName = "Endpoints - MapUserRoleEndpoints n�o lan�a exce��o")]
        [Trait("Api", "")]
        public void MapUserRoleEndpoints_DoesNotThrow()
        {
            var routeBuilder = CreateRouteBuilder();
            var ex = Record.Exception(() => UserRoleEndpoint.MapUserRoleEndpoints(routeBuilder));
            Assert.Null(ex);
        }

        [Fact(DisplayName = "Endpoints - MapJobEndpoints n�o lan�a exce��o")]
        [Trait("Api", "")]
        public void MapJobEndpoints_DoesNotThrow()
        {
            var routeBuilder = CreateRouteBuilder();
            var ex = Record.Exception(() => JobEndpoint.MapJobEndpoints(routeBuilder));
            Assert.Null(ex);
        }

        [Fact(DisplayName = "Endpoints - MapJwtKeyEndpoints n�o lan�a exce��o")]
        [Trait("Api", "")]
        public void MapJwtKeyEndpoints_DoesNotThrow()
        {
            var routeBuilder = CreateRouteBuilder();
            var ex = Record.Exception(() => JwtKeyEndpoint.MapJwtKeyEndpoints(routeBuilder));
            Assert.Null(ex);
        }

        [Fact(DisplayName = "Endpoints - MapAuthEndpoints n�o lan�a exce��o")]
        [Trait("Api", "")]
        public void MapAuthEndpoints_DoesNotThrow()
        {
            var routeBuilder = CreateRouteBuilder();
            var ex = Record.Exception(() => AuthEndpoint.MapAuthEndpoints(routeBuilder));
            Assert.Null(ex);
        }

        [Fact(DisplayName = "Endpoints - MapTenantContactPersonEndpoints - does not throw")]
        [Trait("Api", "")]
        public void MapTenantContactPersonEndpoints_DoesNotThrow()
        {
            var routeBuilder = CreateRouteBuilder();
            var ex = Record.Exception(() => TenantContactPersonEndpoint.MapTenantContactPersonEndpoints(routeBuilder));
            Assert.Null(ex);
        }
    }
}
