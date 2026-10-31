using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Validators.Job;
using VianaHub.Global.Gerit.Infra.Integration.Messaging;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Security;
using VianaHub.Global.Gerit.Infra.Job.Services;
using VianaHub.Global.Gerit.Infra.Job.HostedServices;
using VianaHub.Global.Gerit.Domain.Interfaces.Job;
using VianaHub.Global.Gerit.Infra.Job.Interfaces;
using VianaHub.Global.Gerit.Application.Services.Identity;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Services.Identity;
using VianaHub.Global.Gerit.Infra.Data.Repository.Identity;
using VianaHub.Global.Gerit.Application.Interfaces.Billing;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Interfaces.Job;
using VianaHub.Global.Gerit.Application.Services.Billing;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Application.Services.Common;
using VianaHub.Global.Gerit.Application.Services.Job;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Entities.Job;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Services.Billing;
using VianaHub.Global.Gerit.Domain.Services.Business;
using VianaHub.Global.Gerit.Domain.Validators.Billing.Plan;
using VianaHub.Global.Gerit.Domain.Validators.Billing.Subscription;
using VianaHub.Global.Gerit.Domain.Validators.Billing.Tenant;
using VianaHub.Global.Gerit.Domain.Validators.Identity.Action;
using VianaHub.Global.Gerit.Domain.Validators.Identity.Resource;
using VianaHub.Global.Gerit.Domain.Validators.Identity.Role;
using VianaHub.Global.Gerit.Domain.Validators.Identity.RolePermission;
using VianaHub.Global.Gerit.Domain.Validators.Identity.User;
using VianaHub.Global.Gerit.Domain.Validators.Identity.Jwt;
using VianaHub.Global.Gerit.Domain.Validators.Identity.UserRole;
using VianaHub.Global.Gerit.Domain.Validators.Business.Vehicle;
using VianaHub.Global.Gerit.Domain.Validators.Business.Equipment;
using VianaHub.Global.Gerit.Domain.Validators.Business.Function;
using VianaHub.Global.Gerit.Domain.Validators.Business.Team;
using VianaHub.Global.Gerit.Domain.Validators.Business.Employee;
using VianaHub.Global.Gerit.Domain.Validators.Business.EmployeeAddress;
using VianaHub.Global.Gerit.Domain.Validators.Business.EmployeeContact;
using VianaHub.Global.Gerit.Domain.Validators.Business.Client;
using VianaHub.Global.Gerit.Domain.Validators.Business.ClientAddress;
using VianaHub.Global.Gerit.Domain.Validators.Business.ClientContact;
using VianaHub.Global.Gerit.Domain.Validators.Business.Visit;
using VianaHub.Global.Gerit.Domain.Validators.Business.VisitContactPersons;
using VianaHub.Global.Gerit.Domain.Validators.Business.EquipmentType;
using VianaHub.Global.Gerit.Domain.Validators.Business.AddressType;
using VianaHub.Global.Gerit.Infra.Data.Repository.Business;
using VianaHub.Global.Gerit.Infra.Data.Repository.Billing;
using VianaHub.Global.Gerit.Infra.Data.Repository.Job;
using VianaHub.Global.Gerit.Infra.Job.Jobs.Maintenance;
using VianaHub.Global.Gerit.Infra.Job.Jobs.Security;
using VianaHub.Global.Gerit.Domain.Validators.Business.VisitAddress;
using VianaHub.Global.Gerit.Domain.Validators.Business.EmployeeTeam;
using VianaHub.Global.Gerit.Domain.Validators.Business.VisitTeam;
using VianaHub.Global.Gerit.Domain.Validators.Business.VisitTeamEmployee;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Validators.Business.VisitTeamVehicle;
using VianaHub.Global.Gerit.Domain.Validators.Business.VisitTeamEquipment;
using VianaHub.Global.Gerit.Domain.Validators.Identity.UserPreferences;
using VianaHub.Global.Gerit.Domain.Validators.Business.AcquisitionSourceType;
using VianaHub.Global.Gerit.Domain.Validators.Business.FileType;
using VianaHub.Global.Gerit.Domain.Validators.Business.VisitAttachment;
using VianaHub.Global.Gerit.Domain.Validators.Business.ClientFiscalData;
using VianaHub.Global.Gerit.Domain.Validators.Business.EmployeeFiscalData;
using VianaHub.Global.Gerit.Domain.Validators.Billing.TenantContactPerson;
using VianaHub.Global.Gerit.Domain.Validators.Billing.TenantFiscalData;
using VianaHub.Global.Gerit.Domain.Validators.Billing.TenantAddress;
using VianaHub.Global.Gerit.Domain.Validators.Business.ClientDocument;
using VianaHub.Global.Gerit.Domain.Validators.Business.PartyType;
using VianaHub.Global.Gerit.Domain.Validators.Business.StatusDomain;
using VianaHub.Global.Gerit.Domain.Validators.Business.StatusDefinition;
using VianaHub.Global.Gerit.Domain.Validators.Business.DocumentType;

namespace VianaHub.Global.Gerit.Infra.IoC;

/// <summary>
/// Configuração centralizada de injeção de dependências para todas as camadas do Gerit.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra todos os serviços da aplicação no container de DI.
    /// </summary>
    public static IServiceCollection AddGeritInfrastructure(this IServiceCollection services)
    {
        // Notificações (Scoped para manter estado durante a requisição)
        services.AddScoped<INotify, Notify>();

        // Contexto de Tenant para requests não autenticados (ex: login, register)
        // Scoped: vive durante o ciclo de vida do request HTTP
        services.AddScoped<IRequestTenantContext, RequestTenantContext>();

        // Contexto de Idioma para traduções i18n (ex: resolver nomes traduzidos)
        // Scoped: vive durante o ciclo de vida do request HTTP
        services.AddScoped<IRequestLanguageContext, RequestLanguageContext>();

        // Validators (Scoped)
        services.AddScoped<IValidator<UserRolesEntity>, UserRoleValidator>();
        services.AddScoped<IValidator<RolePermissionsEntity>, RolePermissionValidator>();

        services.AddScoped<IEntityDomainValidator<ActionEntity>, ActionValidator>();
        services.AddScoped<IEntityDomainValidator<ResourceEntity>, ResourceValidator>();
        services.AddScoped<IEntityDomainValidator<RoleEntity>, RoleValidator>();
        services.AddScoped<IEntityDomainValidator<SubscriptionPlanEntity>, PlanValidator>();
        services.AddScoped<IEntityDomainValidator<TenantEntity>, TenantValidator>();
        services.AddScoped<IEntityDomainValidator<SubscriptionEntity>, SubscriptionValidator>();
        services.AddScoped<IEntityDomainValidator<UserEntity>, UserValidator>();
        services.AddScoped<IEntityDomainValidator<UserPreferencesEntity>, UserPreferencesValidator>();
        services.AddScoped<IEntityDomainValidator<JobDefinitionsEntity>, JobDefinitionValidator>();
        services.AddScoped<IEntityDomainValidator<JwtKeysEntity>, JwtKeyValidator>();
        services.AddScoped<IEntityDomainValidator<VehicleEntity>, VehicleValidator>();
        services.AddScoped<IEntityDomainValidator<EquipmentEntity>, EquipmentValidator>();
        services.AddScoped<IEntityDomainValidator<EquipmentTypeEntity>, EquipmentTypeValidator>();
        services.AddScoped<IEntityDomainValidator<VisitTeamFunctionsEntity>, VisitTeamFunctionValidator>();
        services.AddScoped<IEntityDomainValidator<TeamEntity>, TeamValidator>();
        services.AddScoped<IEntityDomainValidator<EmployeeEntity>, EmployeeValidator>();
        services.AddScoped<IEntityDomainValidator<EmployeeAddressesEntity>, EmployeeAddressValidator>();
        services.AddScoped<IEntityDomainValidator<EmployeeContactPersonsEntity>, EmployeeContactPersonValidator>();
        services.AddScoped<IEntityDomainValidator<AddressTypeEntity>, AddressTypeValidator>();
        services.AddScoped<IEntityDomainValidator<FileTypeEntity>, FileTypeValidator>();
        services.AddScoped<IEntityDomainValidator<AcquisitionSourceTypeEntity>, AcquisitionSourceTypeValidator>();
        services.AddScoped<IEntityDomainValidator<ClientEntity>, ClientValidator>();
        services.AddScoped<IEntityDomainValidator<ClientAddressesEntity>, ClientAddressValidator>();
        services.AddScoped<IEntityDomainValidator<ClientContactPersonsEntity>, ClientContactValidator>();
        services.AddScoped<IEntityDomainValidator<VisitContactPersonsEntity>, VisitContactPersonValidator>();
        services.AddScoped<IEntityDomainValidator<VisitEntity>, VisitValidator>();
        services.AddScoped<IEntityDomainValidator<VisitAddressesEntity>, VisitAddressValidator>();
        services.AddScoped<IEntityDomainValidator<EmployeeTeamEntity>, EmployeeTeamValidator>();
        services.AddScoped<IEntityDomainValidator<VisitTeamEntity>, VisitTeamValidator>();
        services.AddScoped<IEntityDomainValidator<VisitTeamEmployeeEntity>, VisitTeamEmployeeValidator>();
        services.AddScoped<IEntityDomainValidator<VisitTeamVehicleEntity>, VisitTeamVehicleValidator>();
        services.AddScoped<IEntityDomainValidator<VisitTeamEquipmentEntity>, VisitTeamEquipmentValidator>();
        services.AddScoped<IEntityDomainValidator<VisitAttachmentsEntity>, VisitAttachmentValidator>();
        services.AddScoped<IEntityDomainValidator<ClientFiscalDataEntity>, ClientFiscalDataValidator>();
        services.AddScoped<IEntityDomainValidator<EmployeeFiscalDataEntity>, EmployeeFiscalDataValidator>();
        services.AddScoped<IEntityDomainValidator<TenantContactPersonsEntity>, TenantContactValidator>();
        services.AddScoped<IEntityDomainValidator<TenantFiscalDataEntity>, TenantFiscalDataValidator>();
        services.AddScoped<IEntityDomainValidator<TenantAddressesEntity>, TenantAddressValidator>();
        services.AddScoped<IEntityDomainValidator<ClientDocumentsEntity>, ClientDocumentValidator>();
        services.AddScoped<IEntityDomainValidator<PartyTypeEntity>, PartyTypeValidator>();
        services.AddScoped<IEntityDomainValidator<StatusDomainEntity>, StatusDomainValidator>();
        services.AddScoped<IEntityDomainValidator<StatusDefinitionEntity>, StatusDefinitionValidator>();
        services.AddScoped<IEntityDomainValidator<DocumentTypeEntity>, DocumentTypeValidator>();


        // Application - Common Services
        services.AddScoped<IFileValidationService, FileValidationService>();

        // Application - App Services
        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddScoped<IUserRoleAppService, UserRoleAppService>();
        services.AddScoped<IJwtKeyAppService, JwtKeyAppService>();
        services.AddScoped<IAddressTypeAppService, AddressTypeAppService>();
        services.AddScoped<IFileTypeAppService, FileTypeAppService>();
        services.AddScoped<IAcquisitionSourceTypeAppService, AcquisitionSourceTypeAppService>();
        services.AddScoped<IStatusDefinitionAppService, StatusDefinitionAppService>();
        services.AddScoped<IDocumentTypeAppService, DocumentTypeAppService>();
        services.AddScoped<IPartyTypeAppService, PartyTypeAppService>();
        services.AddScoped<IStatusDomainAppService, StatusDomainAppService>();
        services.AddScoped<IVisitTeamFunctionAppService, VisitTeamFunctionAppService>();
        services.AddScoped<ITeamAppService, TeamAppService>();
        services.AddScoped<IVehicleAppService, VehicleAppService>();
        services.AddScoped<IEquipmentAppService, EquipmentAppService>();
        services.AddScoped<IEquipmentTypeAppService, EquipmentTypeAppService>();
        services.AddScoped<IEmployeeAppService, EmployeeAppService>();
        services.AddScoped<IEmployeeTeamsAppService, EmployeeTeamAppService>();
        services.AddScoped<IEmployeeTeamDataRepository, EmployeeTeamDataRepository>();
        services.AddScoped<IVisitTeamsAppService, VisitTeamAppService>();
        services.AddScoped<IVisitTeamDataRepository, VisitTeamDataRepository>();
        services.AddScoped<IVisitTeamEmployeeAppService, VisitTeamEmployeeAppService>();
        services.AddScoped<IVisitTeamVehiclesAppService, VisitTeamVehicleAppService>();
        services.AddScoped<IVisitTeamEquipmentsAppService, VisitTeamEquipmentAppService>();
        services.AddScoped<IVisitAttachmentAppService, VisitAttachmentAppService>();
        services.AddScoped<IClientAppService, ClientAppService>();
        services.AddScoped<IClientAddressAppService, ClientAddressAppService>();
        services.AddScoped<IClientContactPersonAppService, ClientContactPersonAppService>();
        services.AddScoped<IClientFiscalDataAppService, ClientFiscalDataAppService>();
        services.AddScoped<IEmployeeFiscalDataAppService, EmployeeFiscalDataAppService>();
        services.AddScoped<IEmployeeAddressAppService, EmployeeAddressAppService>();
        services.AddScoped<IEmployeeContactPersonAppService, EmployeeContactPersonAppService>();
        services.AddScoped<IVisitContactPersonAppService, VisitContactPersonAppService>();
        services.AddScoped<IVisitAddressAppService, VisitAddressAppService>();
        services.AddScoped<IVisitAppService, VisitAppService>();
        services.AddScoped<IActionAppService, ActionAppService>();
        services.AddScoped<IResourceAppService, ResourceAppService>();
        services.AddScoped<IRoleAppService, RoleAppService>();
        services.AddScoped<IRolePermissionAppService, RolePermissionAppService>();
        services.AddScoped<IPlanAppService, PlanAppService>();
        services.AddScoped<ITenantAppService, TenantAppService>();
        services.AddScoped<ITenantContactPersonAppService, TenantContactPersonAppService>();
        services.AddScoped<ITenantFiscalDataAppService, TenantFiscalDataAppService>();
        services.AddScoped<ITenantAddressesAppService, TenantAddressesAppService>();
        services.AddScoped<ISubscriptionAppService, SubscriptionAppService>();
        services.AddScoped<IUserAppService, UserAppService>();
        services.AddScoped<IUserPreferencesAppService, UserPreferencesAppService>();
        services.AddScoped<IJobAppService, JobAppService>();

        // Domain
        services.AddScoped<IUserRoleDomainService, UserRoleDomainService>();
        services.AddScoped<IVisitTeamDomainService, VisitTeamDomainService>();
        services.AddScoped<IVisitTeamEmployeeDomainService, VisitTeamEmployeeDomainService>();
        services.AddScoped<IActionDomainService, ActionDomainService>();
        services.AddScoped<IEmployeeTeamDomainService, EmployeeTeamDomainService>();
        services.AddScoped<IResourceDomainService, ResourceDomainService>();
        services.AddScoped<IAddressTypeDomainService, AddressTypeDomainService>();
        services.AddScoped<IFileTypeDomainService, FileTypeDomainService>();
        services.AddScoped<IAcquisitionSourceTypeDomainService, AcquisitionSourceTypeDomainService>();
        services.AddScoped<IRoleDomainService, RoleDomainService>();
        services.AddScoped<IPlanDomainService, PlanDomainService>();
        services.AddScoped<ITenantDomainService, TenantDomainService>();
        services.AddScoped<ITenantContactPersonDomainService, TenantContactDomainService>();
        services.AddScoped<ITenantFiscalDataDomainService, TenantFiscalDataDomainService>();
        services.AddScoped<ITenantAddressesDomainService, TenantAddressesDomainService>();
        services.AddScoped<ISubscriptionDomainService, SubscriptionDomainService>();
        services.AddScoped<IUserDomainService, UserDomainService>();
        services.AddScoped<IUserPreferencesDomainService, UserPreferencesDomainService>();
        services.AddScoped<IRolePermissionDomainService, RolePermissionDomainService>();
        services.AddScoped<IJwtKeyDomainService, JwtKeyDomainService>();
        services.AddScoped<IVisitTeamFunctionDomainService, VisitTeamFunctionDomainService>();
        services.AddScoped<ITeamDomainService, TeamDomainService>();
        services.AddScoped<IVehicleDomainService, VehicleDomainService>();
        services.AddScoped<IEquipmentDomainService, EquipmentDomainService>();
        services.AddScoped<IEquipmentTypeDomainService, EquipmentTypeDomainService>();
        services.AddScoped<IEmployeeDomainService, EmployeeDomainService>();
        services.AddScoped<IEmployeeAddressDomainService, EmployeeAddressDomainService>();
        services.AddScoped<IEmployeeContactPersonDomainService, EmployeeContactPersonDomainService>();
        services.AddScoped<IClientDomainService, ClientDomainService>();
        services.AddScoped<IClientAddressDomainService, ClientAddressDomainService>();
        services.AddScoped<IClientContactPersonDomainService, ClientContactPersonDomainService>();
        services.AddScoped<IVisitContactDomainService, VisitContactPersonDomainService>();
        services.AddScoped<IVisitAddressDomainService, VisitAddressDomainService>();
        services.AddScoped<IVisitDomainService, VisitDomainService>();
        services.AddScoped<IVisitTeamVehicleDomainService, VisitTeamVehicleDomainService>();
        services.AddScoped<IVisitTeamEquipmentDomainService, VisitTeamEquipmentDomainService>();
        services.AddScoped<IVisitAttachmentDomainService, VisitAttachmentDomainService>();
        services.AddScoped<IClientFiscalDataDomainService, ClientFiscalDataDomainService>();
        services.AddScoped<IEmployeeFiscalDataDomainService, EmployeeFiscalDataDomainService>();
        services.AddScoped<IPartyTypeDomainService, PartyTypeDomainService>();
        services.AddScoped<IStatusDomainDomainService, StatusDomainDomainService>();
        services.AddScoped<IStatusDefinitionDomainService, StatusDefinitionDomainService>();
        services.AddScoped<IDocumentTypeDomainService, DocumentTypeDomainService>();

        // Infra.Data - Repositories
        services.AddScoped<IAddressTypeDataRepository, AddressTypeDataRepository>();
        services.AddScoped<IFileTypeDataRepository, FileTypeDataRepository>();
        services.AddScoped<IAcquisitionSourceTypeDataRepository, AcquisitionSourceTypeDataRepository>();
        services.AddScoped<IVisitTeamFunctionDataRepository, VisitTeamFunctionDataRepository>();
        services.AddScoped<ITeamDataRepository, TeamDataRepository>();
        services.AddScoped<IVehicleDataRepository, VehicleDataRepository>();
        services.AddScoped<IEquipmentDataRepository, EquipmentDataRepository>();
        services.AddScoped<IEquipmentTypeDataRepository, EquipmentTypeDataRepository>();
        services.AddScoped<IEmployeeDataRepository, EmployeeDataRepository>();
        services.AddScoped<IEmployeeAddressDataRepository, EmployeeAddressDataRepository>();
        services.AddScoped<IEmployeeContactPersonDataRepository, EmployeeContactPersonDataRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IClientDataRepository, ClientDataRepository>();
        services.AddScoped<IClientContactPersonDataRepository, ClientContactPersonRepository>();
        services.AddScoped<IClientAddressDataRepository, ClientAddressRepository>();
        services.AddScoped<IClientFiscalDataDataRepository, ClientFiscalDataDataRepository>();
        services.AddScoped<IEmployeeFiscalDataDataRepository, EmployeeFiscalDataDataRepository>();
        services.AddScoped<IVisitContactDataRepository, VisitContactPersonDataRepository>();
        services.AddScoped<IVisitAddressDataRepository, VisitAddressDataRepository>();
        services.AddScoped<IVisitDataRepository, VisitDataRepository>();
        services.AddScoped<IActionDataRepository, ActionDataRepository>();
        services.AddScoped<IResourceDataRepository, ResourceDataRepository>();
        services.AddScoped<IRoleDataRepository, RoleDataRepository>();
        services.AddScoped<IPlanDataRepository, PlanDataRepository>();
        services.AddScoped<ITenantDataRepository, TenantDataRepository>();
        services.AddScoped<ITenantContactPersonDataRepository, TenantContactPersonDataRepository>();
        services.AddScoped<ITenantFiscalDataDataRepository, TenantFiscalDataDataRepository>();
        services.AddScoped<ITenantAddressesDataRepository, TenantAddressesDataRepository>();
        services.AddScoped<ISubscriptionDataRepository, SubscriptionDataRepository>();
        services.AddScoped<IJwtKeyDataRepository, JwtKeyDataRepository>();
        services.AddSingleton<IJwtSystemJobContext, JwtSystemJobContext>();
        services.AddScoped<IJwtSystemJobTenantDiscovery, SqlJwtSystemJobTenantDiscovery>();
        services.AddScoped<IUserDataRepository, UserDataRepository>();
        services.AddScoped<IUserPreferencesDataRepository, UserPreferencesDataRepository>();
        services.AddScoped<IUserRoleDataRepository, UserRoleDataRepository>();
        services.AddScoped<IRolePermissionDataRepository, RolePermissionDataRepository>();
        services.AddScoped<IRefreshTokenDataRepository, RefreshTokenDataRepository>();
        services.AddScoped<IJobDefinitionDataRepository, JobDefinitionDataRepository>();
        services.AddScoped<IVisitTeamDataRepository, VisitTeamDataRepository>();
        services.AddScoped<IVisitTeamEmployeeDataRepository, VisitTeamEmployeeDataRepository>();
        services.AddScoped<IVisitTeamVehicleDataRepository, VisitTeamVehicleDataRepository>();
        services.AddScoped<IVisitTeamEquipmentDataRepository, VisitTeamEquipmentDataRepository>();
        services.AddScoped<IVisitAttachmentDataRepository, VisitAttachmentDataRepository>();
        services.AddScoped<IPartyTypeDataRepository, PartyTypeDataRepository>();
        services.AddScoped<IPartyTypeTranslationDataRepository, PartyTypeTranslationDataRepository>();
        services.AddScoped<IStatusDomainDataRepository, StatusDomainDataRepository>();
        services.AddScoped<IStatusDefinitionDataRepository, StatusDefinitionDataRepository>();
        services.AddScoped<IDocumentTypeDataRepository, DocumentTypeDataRepository>();
        services.AddScoped<ISubscriptionPlanFileRuleDataRepository, SubscriptionPlanFileRuleDataRepository>();

        // Infra.Messaging (Email sender no-op por enquanto)
        services.AddScoped<IEmailSender, NoOpEmailSender>();

        // Hangfire Job service
        services.AddScoped<IJobSchedulerService, HangfireJobService>();
        services.AddScoped<IJobExecutor, HangfireJobExecutor>();
        services.AddScoped<IJobSyncService, JobSyncService>();
        services.AddScoped<ScheduledSyncJobDefinitionsJob>();
        services.AddScoped<JwtKeyRotationJob>();
        services.AddHostedService<JobSyncHostedService>();

        // Data Context
        services.AddScoped<GeritDbContext>();

        // Secret provider (chave mestra gerenciada externamente) - por padrão lê variável de ambiente JWT_MASTER_KEY
        services.AddSingleton<ISecretProvider, SecretProviderEnvironment>();

        return services;
    }
}


