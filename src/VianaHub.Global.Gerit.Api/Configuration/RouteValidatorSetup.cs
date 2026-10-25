using VianaHub.Global.Gerit.Application.Dtos.Request.Business.AddressType;
using VianaHub.Global.Gerit.Api.Validators.Business.AddressType;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.PartyType;
using VianaHub.Global.Gerit.Api.Validators.Business.PartyType;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDomain;
using VianaHub.Global.Gerit.Api.Validators.Business.StatusDomain;
using FluentValidation;
using VianaHub.Global.Gerit.Api.Validators.Billing.Plan;
using VianaHub.Global.Gerit.Api.Validators.Billing.Subscription;
using VianaHub.Global.Gerit.Api.Validators.Billing.Tenant;
using VianaHub.Global.Gerit.Api.Validators.Business.Equipment;
using VianaHub.Global.Gerit.Api.Validators.Business.Function;
using VianaHub.Global.Gerit.Api.Validators.Business.Vehicle;
using VianaHub.Global.Gerit.Api.Validators.Identity.Action;
using VianaHub.Global.Gerit.Api.Validators.Identity.Auth;
using VianaHub.Global.Gerit.Api.Validators.Identity.Resource;
using VianaHub.Global.Gerit.Api.Validators.Identity.Role;
using VianaHub.Global.Gerit.Api.Validators.Identity.User;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.Plan;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.Subscription;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.Tenant;
// Equipment DTOs & Validators
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Equipment;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Function;
// Vehicle DTOs & Validators
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Vehicle;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Action;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Auth;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Resource;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Role;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.User;

// EmployeeTeams DTOs & Validators
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.EmployeeTeams;
using VianaHub.Global.Gerit.Api.Validators.Business.EmployeeTeams;

// VisitTeams DTOs & Validators
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeams;
using VianaHub.Global.Gerit.Api.Validators.Business.VisitTeams;

// VisitTeamVehicles DTOs & Validators
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeamVehicles;
using VianaHub.Global.Gerit.Api.Validators.Business.VisitTeamVehicles;

// UserPreferences DTOs & Validators
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.UserPreferences;
using VianaHub.Global.Gerit.Api.Validators.Identity.UserPreferences;

// AcquisitionSourceType DTOs & Validators
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.AcquisitionSourceType;
using VianaHub.Global.Gerit.Api.Validators.Business.AcquisitionSourceType;

// StatusDefinition DTOs & Validators
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDefinition;
using VianaHub.Global.Gerit.Api.Validators.Business.StatusDefinition;

// DocumentType DTOs & Validators
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.DocumentType;
using VianaHub.Global.Gerit.Api.Validators.Business.DocumentType;

// Client DTOs & Validators
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Client;
using VianaHub.Global.Gerit.Api.Validators.Business.Client;

// TenantContact DTOs & Validators
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.TenantContactPerson;
using VianaHub.Global.Gerit.Api.Validators.Billing.TenantContactPerson;

// TenantFiscalData DTOs & Validators
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.TenantFiscalData;
using VianaHub.Global.Gerit.Api.Validators.Billing.TenantFiscalData;

// TenantAddress DTOs & Validators
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.TenantAddress;
using VianaHub.Global.Gerit.Api.Validators.Billing.TenantAddress;

namespace VianaHub.Global.Gerit.Api.Configuration;

/// <summary>
/// Classe responsável por registrar os validadores de rota na injeção de dependência.
/// </summary>
public static class RouteValidatorSetup
{
    /// <summary>
    /// Adiciona os validadores de rota à coleção de serviços.
    /// </summary>
    /// <param name="services">A coleção de serviços da aplicação.</param>
    /// <returns>A própria coleção de serviços para encadeamento.</returns>
    public static IServiceCollection AddRouteValidatorSetup(this IServiceCollection services)
    {
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Action Route Validators
        services.AddScoped<IValidator<CreateActionRequest>, CreateActionRouteValidator>();
        services.AddScoped<IValidator<UpdateActionRequest>, UpdateActionRouteValidator>();

        // Resource Route Validators
        services.AddScoped<IValidator<CreateResourceRequest>, CreateResourceRouteValidator>();
        services.AddScoped<IValidator<UpdateResourceRequest>, UpdateResourceRouteValidator>();

        // Role Route Validators
        services.AddScoped<IValidator<CreateRoleRequest>, CreateRoleRouteValidator>();
        services.AddScoped<IValidator<UpdateRoleRequest>, UpdateRoleRouteValidator>();

        // Plan Route Validators
        services.AddScoped<IValidator<CreatePlanRequest>, CreatePlanRouteValidator>();
        services.AddScoped<IValidator<UpdatePlanRequest>, UpdatePlanRouteValidator>();

        // Tenant Route Validators
        services.AddScoped<IValidator<CreateTenantRequest>, CreateTenantRouteValidator>();
        services.AddScoped<IValidator<UpdateTenantRequest>, UpdateTenantRouteValidator>();

        // TenantContact Route Validators
        services.AddScoped<IValidator<CreateTenantContactPersonRequest>, CreateTenantContactPersonRouteValidator>();
        services.AddScoped<IValidator<UpdateTenantContactPersonRequest>, UpdateTenantContactPersonRouteValidator>();

        // TenantFiscalData Route Validators
        services.AddScoped<IValidator<CreateTenantFiscalDataRequest>, CreateTenantFiscalDataRouteValidator>();
        services.AddScoped<IValidator<UpdateTenantFiscalDataRequest>, UpdateTenantFiscalDataRouteValidator>();

        // TenantAddress Route Validators
        services.AddScoped<IValidator<CreateTenantAddressRequest>, CreateTenantAddressRouteValidator>();
        services.AddScoped<IValidator<UpdateTenantAddressRequest>, UpdateTenantAddressRouteValidator>();

        // User Route Validators
        services.AddScoped<IValidator<CreateUserRequest>, CreateUserRouteValidator>();
        services.AddScoped<IValidator<UpdateUserRequest>, UpdateUserRouteValidator>();
        services.AddScoped<IValidator<UpdatePasswordRequest>, UpdatePasswordRouteValidator>();

        // Subscription Route Validators
        services.AddScoped<IValidator<CreateSubscriptionRequest>, CreateSubscriptionRouteValidator>();
        services.AddScoped<IValidator<UpdateSubscriptionRequest>, UpdateSubscriptionRouteValidator>();

        // Auth Route Validators
        services.AddScoped<IValidator<RegisterRequest>, RegisterRouteValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginRouteValidator>();
        services.AddScoped<IValidator<RefreshRequest>, RefreshRouteValidator>();

        // Function Route Validators
        services.AddScoped<IValidator<CreateVisitTeamFunctionRequest>, CreateVisitTeamFunctionRouteValidator>();
        services.AddScoped<IValidator<UpdateVisitTeamFunctionRequest>, UpdateVisitTeamFunctionRouteValidator>();

        // Vehicle Route Validators
        services.AddScoped<IValidator<CreateVehicleRequest>, CreateVehicleRouteValidator>();
        services.AddScoped<IValidator<UpdateVehicleRequest>, UpdateVehicleRouteValidator>();

        // Equipment Route Validators
        services.AddScoped<IValidator<CreateEquipmentRequest>, CreateEquipmentRouteValidator>();
        services.AddScoped<IValidator<UpdateEquipmentRequest>, UpdateEquipmentRouteValidator>();

        // AcquisitionSourceType Route Validators
        services.AddScoped<IValidator<CreateAcquisitionSourceTypeRequest>, CreateAcquisitionSourceTypeRouteValidator>();
        services.AddScoped<IValidator<UpdateAcquisitionSourceTypeRequest>, UpdateAcquisitionSourceTypeRouteValidator>();

        // EmployeeTeams Route Validators
        services.AddScoped<IValidator<CreateEmployeeTeamRequest>, CreateEmployeeTeamRouteValidator>();
        services.AddScoped<IValidator<UpdateEmployeeTeamRequest>, UpdateEmployeeTeamRouteValidator>();

        // VisitTeams Route Validators
        services.AddScoped<IValidator<CreateVisitTeamRequest>, CreateVisitTeamRouteValidator>();
        services.AddScoped<IValidator<UpdateVisitTeamRequest>, UpdateVisitTeamRouteValidator>();
        services.AddScoped<IValidator<IFormFile>, BulkUploadVisitTeamsRouteValidator>();

        // VisitTeamVehicles Route Validators
        services.AddScoped<IValidator<CreateVisitTeamVehicleRequest>, CreateVisitTeamVehicleRouteValidator>();
        services.AddScoped<IValidator<UpdateVisitTeamVehicleRequest>, UpdateVisitTeamVehicleRouteValidator>();
        services.AddScoped<IValidator<IFormFile>, BulkUploadVisitTeamVehiclesRouteValidator>();

        // UserPreferences Route Validators
        services.AddScoped<IValidator<CreateUserPreferencesRequest>, CreateUserPreferencesRouteValidator>();
        services.AddScoped<IValidator<UpdateUserPreferencesRequest>, UpdateUserPreferencesRouteValidator>();

        // Client Route Validators
        services.AddScoped<IValidator<CreateClientRequest>, CreateClientRouteValidator>();

        // StatusDefinition Route Validators
        services.AddScoped<IValidator<CreateStatusDefinitionRequest>, CreateStatusDefinitionRouteValidator>();
        services.AddScoped<IValidator<UpdateStatusDefinitionRequest>, UpdateStatusDefinitionRouteValidator>();

        // Validators dos novos contratos multilingues de catálogo
        services.AddScoped<IValidator<CreateAddressTypeRequest>, CreateAddressTypeRouteValidator>();
        services.AddScoped<IValidator<CreatePartyTypeRequest>, CreatePartyTypeRouteValidator>();
        services.AddScoped<IValidator<CreateStatusDomainRequest>, CreateStatusDomainRouteValidator>();

        // DocumentType Route Validators
        services.AddScoped<IValidator<CreateDocumentTypeRequest>, CreateDocumentTypeRouteValidator>();
        services.AddScoped<IValidator<UpdateDocumentTypeRequest>, UpdateDocumentTypeRouteValidator>();

        return services;
    }
}
