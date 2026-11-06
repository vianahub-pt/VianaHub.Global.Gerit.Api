using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Infra.Data.Context;

public class GeritDbContext : DbContext
{
    public GeritDbContext(DbContextOptions<GeritDbContext> options)
        : base(options)
    {
    }

    #region DbSets - Core Multi-Tenant Tables
    public DbSet<SubscriptionPlanEntity> SubscriptionPlans { get; set; }
    public DbSet<SubscriptionPlanTranslationsEntity> SubscriptionPlanTranslations { get; set; }
    public DbSet<SubscriptionPlanFileRulesEntity> SubscriptionPlanFileRules { get; set; }
    public DbSet<SubscriptionEntity> Subscriptions { get; set; }
    public DbSet<TenantEntity> Tenants { get; set; }
    public DbSet<TenantContactPersonsEntity> TenantContactPersons { get; set; }
    public DbSet<TenantAddressesEntity> TenantAddresses { get; set; }
    public DbSet<TenantFiscalDataEntity> TenantFiscalData { get; set; }
    public DbSet<UserEntity> Users { get; set; }
    #endregion

    #region DbSets - RBAC Structure
    public DbSet<RoleEntity> Roles { get; set; }
    public DbSet<ResourceEntity> Resources { get; set; }
    public DbSet<ActionEntity> Actions { get; set; }
    public DbSet<RolePermissionsEntity> RolePermissions { get; set; }
    public DbSet<UserRolesEntity> UserRoles { get; set; }
    public DbSet<JwtKeysEntity> JwtKeys { get; set; }
    #endregion

    #region DbSets - Global Catalog Tables
    public DbSet<PartyTypeEntity> PartyTypes { get; set; }
    public DbSet<PartyTypeTranslationsEntity> PartyTypeTranslations { get; set; }
    public DbSet<AcquisitionSourceTypeEntity> AcquisitionSourceTypes { get; set; }
    public DbSet<AcquisitionSourceTypeTranslationsEntity> AcquisitionSourceTypeTranslations { get; set; }
    #endregion

    #region DbSets - Domain Tables
    public DbSet<AddressTypeEntity> AddressTypes { get; set; }
    public DbSet<AddressTypeTranslationsEntity> AddressTypeTranslations { get; set; }
    public DbSet<ClientEntity> Clients { get; set; }
    public DbSet<ClientContactPersonsEntity> ClientContactPersons { get; set; }
    public DbSet<ClientAddressesEntity> ClientAddresses { get; set; }
    public DbSet<ClientFiscalDataEntity> ClientFiscalData { get; set; }
    public DbSet<TeamEntity> Teams { get; set; }
    public DbSet<EmployeeEntity> Employees { get; set; }
    public DbSet<EmployeeContactPersonsEntity> EmployeeContactPersons { get; set; }
    public DbSet<EmployeeAddressesEntity> EmployeeAddresses { get; set; }
    public DbSet<EmployeeFiscalDataEntity> EmployeeFiscalData { get; set; }
    public DbSet<EmployeeDocumentsEntity> EmployeeDocuments { get; set; }
    public DbSet<EmployeeTeamEntity> EmployeeTeams { get; set; }
    public DbSet<VisitTeamFunctionsEntity> VisitTeamFunctions { get; set; }
    public DbSet<EquipmentEntity> Equipments { get; set; }
    public DbSet<EquipmentTypeEntity> EquipmentTypes { get; set; }
    public DbSet<VehicleEntity> Vehicles { get; set; }
    public DbSet<VisitEntity> Visits { get; set; }
    public DbSet<VisitTeamEntity> VisitTeams { get; set; }
    public DbSet<VisitTeamEmployeeEntity> VisitTeamEmployees { get; set; }
    public DbSet<VisitTeamEquipmentEntity> VisitTeamEquipments { get; set; }
    public DbSet<VisitContactPersonsEntity> VisitContactPersons { get; set; }
    public DbSet<VisitAddressesEntity> VisitAddresses { get; set; }
    public DbSet<VisitTeamVehicleEntity> VisitTeamVehicles { get; set; }
    public DbSet<VisitAttachmentsEntity> VisitAttachments { get; set; }
    public DbSet<DocumentTypeEntity> DocumentTypes { get; set; }
    public DbSet<DocumentTypeTranslationsEntity> DocumentTypeTranslations { get; set; }
    public DbSet<FileTypeEntity> FileTypes { get; set; }
    public DbSet<FileTypeTranslationsEntity> FileTypeTranslations { get; set; }
    public DbSet<StatusDomainEntity> StatusDomains { get; set; }
    public DbSet<StatusDomainTranslationsEntity> StatusDomainTranslations { get; set; }
    public DbSet<StatusDefinitionEntity> StatusDefinitions { get; set; }
    public DbSet<StatusDefinitionTranslationsEntity> StatusDefinitionTranslations { get; set; }
    public DbSet<TenantDocumentsEntity> TenantDocuments { get; set; }
    public DbSet<ClientDocumentsEntity> ClientDocuments { get; set; }

    public DbSet<UserPreferencesEntity> UserPreferences { get; set; }
    public DbSet<RefreshTokensEntity> RefreshTokens { get; set; }
    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica todas as configura��es de mapeamento do assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GeritDbContext).Assembly);

        // Configura o schema padr�o
        modelBuilder.HasDefaultSchema("dbo");

        // Apply a global query filter to all entities that expose an IsDeleted property
        // (bool or nullable bool). This guarantees soft-deleted records are excluded
        // from queries unless IgnoreQueryFilters() is explicitly used.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (clrType == null) continue;

            var prop = clrType.GetProperty("IsDeleted", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null) continue;

            var propType = prop.PropertyType;
            if (propType != typeof(bool) && propType != typeof(bool?)) continue;

            var parameter = Expression.Parameter(clrType, "e");
            var propAccess = Expression.Property(parameter, prop);
            Expression predicate;

            if (propType == typeof(bool))
            {
                predicate = Expression.Not(propAccess);
            }
            else
            {
                // nullable bool: e.IsDeleted == false
                var falseConst = Expression.Constant(false, typeof(bool?));
                predicate = Expression.Equal(propAccess, falseConst);
            }

            var lambda = Expression.Lambda(predicate, parameter);
            modelBuilder.Entity(clrType).HasQueryFilter(lambda);
        }
    }
}
