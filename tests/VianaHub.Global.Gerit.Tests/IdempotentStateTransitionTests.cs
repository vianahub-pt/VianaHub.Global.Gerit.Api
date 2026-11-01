using System.Reflection;
using FluentValidation.Results;
using Moq;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using Apps = VianaHub.Global.Gerit.Application.Services;
using Domains = VianaHub.Global.Gerit.Domain.Services;
using Entities = VianaHub.Global.Gerit.Domain.Entities;
using Validators = VianaHub.Global.Gerit.Domain.Validators;

namespace VianaHub.Global.Gerit.Tests;

public class IdempotentStateTransitionTests
{
    private static readonly string[] Operations = ["Activate", "Deactivate", "Delete"];

    // Casos explícitos provenientes da auditoria; não descobrem nem executam recursos especiais por convenção.
    private static readonly ResourceCase[] Resources =
    [
        new("AcquisitionSourceType", typeof(Entities.Business.AcquisitionSourceTypeEntity), typeof(Validators.Business.AcquisitionSourceType.AcquisitionSourceTypeValidator), typeof(Apps.Business.AcquisitionSourceTypeAppService), typeof(Domains.Business.AcquisitionSourceTypeDomainService), 410),
        new("Action", typeof(Entities.Identity.ActionEntity), typeof(Validators.Identity.Action.ActionValidator), typeof(Apps.Identity.ActionAppService), typeof(Domains.Identity.ActionDomainService), 410),
        new("AddressType", typeof(Entities.Business.AddressTypeEntity), typeof(Validators.Business.AddressType.AddressTypeValidator), typeof(Apps.Business.AddressTypeAppService), typeof(Domains.Business.AddressTypeDomainService), 410),
        new("Client", typeof(Entities.Business.ClientEntity), typeof(Validators.Business.Client.ClientValidator), typeof(Apps.Business.ClientAppService), typeof(Domains.Business.ClientDomainService), 410),
        new("ClientAddresses", typeof(Entities.Business.ClientAddressesEntity), typeof(Validators.Business.ClientAddress.ClientAddressValidator), typeof(Apps.Business.ClientAddressAppService), typeof(Domains.Business.ClientAddressDomainService), 410),
        new("ClientContactPersons", typeof(Entities.Business.ClientContactPersonsEntity), typeof(Validators.Business.ClientContact.ClientContactValidator), typeof(Apps.Business.ClientContactPersonAppService), typeof(Domains.Business.ClientContactPersonDomainService), 410),
        new("ClientDocuments", typeof(Entities.Business.ClientDocumentsEntity), typeof(Validators.Business.ClientDocument.ClientDocumentValidator), null, null, 410),
        new("ClientFiscalData", typeof(Entities.Business.ClientFiscalDataEntity), typeof(Validators.Business.ClientFiscalData.ClientFiscalDataValidator), typeof(Apps.Business.ClientFiscalDataAppService), typeof(Domains.Business.ClientFiscalDataDomainService), 410),
        new("DocumentType", typeof(Entities.Business.DocumentTypeEntity), typeof(Validators.Business.DocumentType.DocumentTypeValidator), typeof(Apps.Business.DocumentTypeAppService), typeof(Domains.Business.DocumentTypeDomainService), 410),
        new("Employee", typeof(Entities.Business.EmployeeEntity), typeof(Validators.Business.Employee.EmployeeValidator), typeof(Apps.Business.EmployeeAppService), typeof(Domains.Business.EmployeeDomainService), 410),
        new("EmployeeAddresses", typeof(Entities.Business.EmployeeAddressesEntity), typeof(Validators.Business.EmployeeAddress.EmployeeAddressValidator), typeof(Apps.Business.EmployeeAddressAppService), typeof(Domains.Business.EmployeeAddressDomainService), 410),
        new("EmployeeContactPersons", typeof(Entities.Business.EmployeeContactPersonsEntity), typeof(Validators.Business.EmployeeContact.EmployeeContactPersonValidator), typeof(Apps.Business.EmployeeContactPersonAppService), typeof(Domains.Business.EmployeeContactPersonDomainService), 410),
        new("EmployeeFiscalData", typeof(Entities.Business.EmployeeFiscalDataEntity), typeof(Validators.Business.EmployeeFiscalData.EmployeeFiscalDataValidator), typeof(Apps.Business.EmployeeFiscalDataAppService), typeof(Domains.Business.EmployeeFiscalDataDomainService), 410),
        new("EmployeeTeam", typeof(Entities.Business.EmployeeTeamEntity), typeof(Validators.Business.EmployeeTeam.EmployeeTeamValidator), typeof(Apps.Business.EmployeeTeamAppService), typeof(Domains.Business.EmployeeTeamDomainService), 410),
        new("Equipment", typeof(Entities.Business.EquipmentEntity), typeof(Validators.Business.Equipment.EquipmentValidator), typeof(Apps.Business.EquipmentAppService), typeof(Domains.Business.EquipmentDomainService), 410),
        new("EquipmentType", typeof(Entities.Business.EquipmentTypeEntity), typeof(Validators.Business.EquipmentType.EquipmentTypeValidator), typeof(Apps.Business.EquipmentTypeAppService), typeof(Domains.Business.EquipmentTypeDomainService), 410),
        new("FileType", typeof(Entities.Business.FileTypeEntity), typeof(Validators.Business.FileType.FileTypeValidator), typeof(Apps.Business.FileTypeAppService), typeof(Domains.Business.FileTypeDomainService), 410),
        new("PartyType", typeof(Entities.Business.PartyTypeEntity), typeof(Validators.Business.PartyType.PartyTypeValidator), typeof(Apps.Business.PartyTypeAppService), typeof(Domains.Business.PartyTypeDomainService), 410),
        new("Resource", typeof(Entities.Identity.ResourceEntity), typeof(Validators.Identity.Resource.ResourceValidator), typeof(Apps.Identity.ResourceAppService), typeof(Domains.Identity.ResourceDomainService), 410),
        new("Role", typeof(Entities.Identity.RoleEntity), typeof(Validators.Identity.Role.RoleValidator), typeof(Apps.Identity.RoleAppService), typeof(Domains.Identity.RoleDomainService), 410),
        new("StatusDefinition", typeof(Entities.Business.StatusDefinitionEntity), typeof(Validators.Business.StatusDefinition.StatusDefinitionValidator), typeof(Apps.Business.StatusDefinitionAppService), typeof(Domains.Business.StatusDefinitionDomainService), 410),
        new("StatusDomain", typeof(Entities.Business.StatusDomainEntity), typeof(Validators.Business.StatusDomain.StatusDomainValidator), typeof(Apps.Business.StatusDomainAppService), typeof(Domains.Business.StatusDomainDomainService), 410),
        new("Subscription", typeof(Entities.Billing.SubscriptionEntity), typeof(Validators.Billing.Subscription.SubscriptionValidator), typeof(Apps.Billing.SubscriptionAppService), typeof(Domains.Billing.SubscriptionDomainService), 404),
        new("SubscriptionPlan", typeof(Entities.Billing.SubscriptionPlanEntity), typeof(Validators.Billing.Plan.PlanValidator), typeof(Apps.Billing.PlanAppService), typeof(Domains.Billing.PlanDomainService), 410),
        new("Team", typeof(Entities.Business.TeamEntity), typeof(Validators.Business.Team.TeamValidator), typeof(Apps.Business.TeamAppService), typeof(Domains.Business.TeamDomainService), 410),
        new("Tenant", typeof(Entities.Billing.TenantEntity), typeof(Validators.Billing.Tenant.TenantValidator), typeof(Apps.Billing.TenantAppService), typeof(Domains.Billing.TenantDomainService), 410),
        new("TenantAddresses", typeof(Entities.Billing.TenantAddressesEntity), typeof(Validators.Billing.TenantAddress.TenantAddressValidator), typeof(Apps.Billing.TenantAddressesAppService), typeof(Domains.Billing.TenantAddressesDomainService), 410),
        new("TenantContactPersons", typeof(Entities.Billing.TenantContactPersonsEntity), typeof(Validators.Billing.TenantContactPerson.TenantContactValidator), typeof(Apps.Billing.TenantContactPersonAppService), typeof(Domains.Billing.TenantContactDomainService), 410),
        new("TenantFiscalData", typeof(Entities.Billing.TenantFiscalDataEntity), typeof(Validators.Billing.TenantFiscalData.TenantFiscalDataValidator), typeof(Apps.Billing.TenantFiscalDataAppService), typeof(Domains.Billing.TenantFiscalDataDomainService), 410),
        new("User", typeof(Entities.Identity.UserEntity), typeof(Validators.Identity.User.UserValidator), typeof(Apps.Identity.UserAppService), typeof(Domains.Identity.UserDomainService), 410),
        new("UserPreferences", typeof(Entities.Identity.UserPreferencesEntity), typeof(Validators.Identity.UserPreferences.UserPreferencesValidator), typeof(Apps.Identity.UserPreferencesAppService), typeof(Domains.Identity.UserPreferencesDomainService), 410),
        new("Vehicle", typeof(Entities.Business.VehicleEntity), typeof(Validators.Business.Vehicle.VehicleValidator), typeof(Apps.Business.VehicleAppService), typeof(Domains.Business.VehicleDomainService), 410),
        new("Visit", typeof(Entities.Business.VisitEntity), typeof(Validators.Business.Visit.VisitValidator), typeof(Apps.Business.VisitAppService), typeof(Domains.Business.VisitDomainService), 410),
        new("VisitAddresses", typeof(Entities.Business.VisitAddressesEntity), typeof(Validators.Business.VisitAddress.VisitAddressValidator), typeof(Apps.Business.VisitAddressAppService), typeof(Domains.Business.VisitAddressDomainService), 410),
        new("VisitAttachments", typeof(Entities.Business.VisitAttachmentsEntity), typeof(Validators.Business.VisitAttachment.VisitAttachmentValidator), typeof(Apps.Business.VisitAttachmentAppService), typeof(Domains.Business.VisitAttachmentDomainService), 410),
        new("VisitContactPersons", typeof(Entities.Business.VisitContactPersonsEntity), typeof(Validators.Business.VisitContactPersons.VisitContactPersonValidator), typeof(Apps.Business.VisitContactPersonAppService), typeof(Domains.Business.VisitContactPersonDomainService), 410),
        new("VisitTeam", typeof(Entities.Business.VisitTeamEntity), typeof(Validators.Business.VisitTeam.VisitTeamValidator), typeof(Apps.Business.VisitTeamAppService), typeof(Domains.Business.VisitTeamDomainService), 410),
        new("VisitTeamEmployee", typeof(Entities.Business.VisitTeamEmployeeEntity), typeof(Validators.Business.VisitTeamEmployee.VisitTeamEmployeeValidator), typeof(Apps.Business.VisitTeamEmployeeAppService), typeof(Domains.Business.VisitTeamEmployeeDomainService), 410),
        new("VisitTeamEquipment", typeof(Entities.Business.VisitTeamEquipmentEntity), typeof(Validators.Business.VisitTeamEquipment.VisitTeamEquipmentValidator), typeof(Apps.Business.VisitTeamEquipmentAppService), typeof(Domains.Business.VisitTeamEquipmentDomainService), 410),
        new("VisitTeamFunctions", typeof(Entities.Business.VisitTeamFunctionsEntity), typeof(Validators.Business.Function.VisitTeamFunctionValidator), typeof(Apps.Business.VisitTeamFunctionAppService), typeof(Domains.Business.VisitTeamFunctionDomainService), 410),
        new("VisitTeamVehicle", typeof(Entities.Business.VisitTeamVehicleEntity), typeof(Validators.Business.VisitTeamVehicle.VisitTeamVehicleValidator), typeof(Apps.Business.VisitTeamVehicleAppService), typeof(Domains.Business.VisitTeamVehicleDomainService), 410),
    ];

    public static IEnumerable<object[]> StateCases => Resources.Where(resource => resource.AppType is not null)
        .SelectMany(resource => new[] { true, false }.SelectMany(activate => new[] { true, false }
            .Select(initiallyActive => new object[] { resource.Name, activate, initiallyActive })));

    public static IEnumerable<object[]> DeleteCases => Resources.SelectMany(resource => new[] { true, false }
        .Select(deleted => new object[] { resource.Name, deleted }));

    public static IEnumerable<object[]> DomainCases => Resources.Where(resource => resource.DomainType is not null)
        .Select(resource => new object[] { resource.Name });

    public static IEnumerable<object[]> AppCases => Resources.Where(resource => resource.AppType is not null)
        .Select(resource => new object[] { resource.Name });

    public static IEnumerable<object[]> LookupCases => Resources.Where(resource => resource.AppType is not null)
        .SelectMany(resource => Operations.Select(operation => new object[] { resource.Name, operation }));

    public static IEnumerable<object[]> IdentityCases => Resources
        .Where(resource => resource.Name is not ("ClientFiscalData" or "EmployeeFiscalData" or "EmployeeTeam" or "VisitAddresses" or "VisitAttachments" or "VisitTeamEmployee"))
        .SelectMany(resource => Operations.Select(operation => new object[] { resource.Name, operation }));

    public static IEnumerable<object[]> DeletedGuardCases => Resources
        .Where(resource => resource.Name is not ("EmployeeTeam" or "VisitAddresses" or "VisitAttachments" or "VisitTeamEmployee" or "VisitTeamEquipment"))
        .SelectMany(resource => new[] { "Activate", "Deactivate" }
            .Where(operation => operation == "Activate" || resource.Name is not ("AcquisitionSourceType" or "AddressType" or "DocumentType" or "FileType" or "PartyType" or "StatusDefinition" or "StatusDomain"))
            .Select(operation => new object[] { resource.Name, operation }));

    public static IEnumerable<object[]> AuditGuardCases => Resources
        .Where(resource => resource.Name is "ClientFiscalData" or "EmployeeFiscalData" or "EmployeeAddresses" or "EmployeeContactPersons" or "VisitContactPersons" or "VisitTeam" or "VisitTeamVehicle")
        .SelectMany(resource => Operations.Select(operation => new object[] { resource.Name, operation }));

    public static IEnumerable<object[]> ParentGuardCases =>
    [
        new object[] { "TenantAddresses", "Activate", "TenantId" },
        new object[] { "TenantAddresses", "Deactivate", "TenantId" },
        new object[] { "TenantAddresses", "Delete", "TenantId" },
        new object[] { "TenantContactPersons", "Activate", "TenantId" },
        new object[] { "TenantContactPersons", "Deactivate", "TenantId" },
        new object[] { "TenantContactPersons", "Delete", "TenantId" },
        new object[] { "TenantFiscalData", "Activate", "TenantId" },
        new object[] { "TenantFiscalData", "Deactivate", "TenantId" },
        new object[] { "TenantFiscalData", "Delete", "TenantId" },
        new object[] { "VisitTeamEmployee", "Activate", "VisitTeamId" },
        new object[] { "VisitTeamEmployee", "Deactivate", "VisitTeamId" },
        new object[] { "VisitTeamEmployee", "Delete", "VisitTeamId" },
        new object[] { "VisitTeamEquipment", "Activate", "VisitTeamId" },
        new object[] { "VisitTeamEquipment", "Deactivate", "VisitTeamId" },
        new object[] { "VisitTeamEquipment", "Delete", "VisitTeamId" },
        new object[] { "VisitTeamVehicle", "Activate", "VisitTeamId" },
        new object[] { "VisitTeamVehicle", "Deactivate", "VisitTeamId" },
        new object[] { "VisitTeamVehicle", "Delete", "VisitTeamId" },
    ];

    [Theory]
    [MemberData(nameof(StateCases))]
    public async Task AppService_StateChangeAndRepeatSucceeds(string resource, bool activate, bool initiallyActive)
    {
        var fixture = new TransitionFixture(Find(resource));
        fixture.Mutate(initiallyActive ? "Activate" : "Deactivate");
        var operation = activate ? "Activate" : "Deactivate";

        for (var repeat = 0; repeat < 2; repeat++)
        {
            Assert.True(await fixture.CallApp(operation));
            Assert.Equal(activate, fixture.GetProperty<bool>("IsActive"));
            Assert.False(fixture.GetProperty<bool>("IsDeleted"));
        }
        Assert.Equal(2, fixture.WriteInvocations.Count());
        fixture.Notify.Verify(notify => notify.Add(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(DeleteCases))]
    public async Task DeleteValidator_AcceptsEitherDeletedState(string resource, bool deleted)
    {
        var fixture = new TransitionFixture(Find(resource));
        if (deleted) fixture.Mutate("Delete");
        Assert.True((await fixture.Validate("Delete")).IsValid);
    }

    [Theory]
    [MemberData(nameof(DomainCases))]
    public async Task DomainDelete_AcceptsPostMutationStateAndRepeat(string resource)
    {
        var fixture = new TransitionFixture(Find(resource));
        fixture.Mutate("Delete");
        Assert.True(await fixture.CallDomain("Delete"));
        Assert.True(await fixture.CallDomain("Delete"));
        Assert.True(fixture.GetProperty<bool>("IsDeleted"));
        Assert.Equal(2, fixture.WriteInvocations.Count());
        fixture.Notify.Verify(notify => notify.Add(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(AppCases))]
    public async Task AppDelete_PreservesLookupContractAfterSoftDelete(string resource)
    {
        var fixture = new TransitionFixture(Find(resource));
        Assert.True(await fixture.CallApp("Delete"));
        Assert.True(fixture.GetProperty<bool>("IsDeleted"));
        Assert.False(await fixture.CallApp("Delete"));
        Assert.Single(fixture.WriteInvocations);
        fixture.AssertLookupNotification();
    }

    [Theory]
    [MemberData(nameof(LookupCases))]
    public async Task MissingResource_DoesNotBecomeArtificialSuccess(string resource, string operation)
    {
        var fixture = new TransitionFixture(Find(resource)) { MissingLookup = true };
        Assert.False(await fixture.CallApp(operation));
        Assert.Empty(fixture.WriteInvocations);
        fixture.AssertLookupNotification();
    }

    [Theory]
    [MemberData(nameof(LookupCases))]
    public async Task DeletedResource_IsNotResurrectedThroughAppService(string resource, string operation)
    {
        var fixture = new TransitionFixture(Find(resource));
        fixture.Mutate("Delete");
        Assert.False(await fixture.CallApp(operation));
        Assert.True(fixture.GetProperty<bool>("IsDeleted"));
        Assert.False(fixture.GetProperty<bool>("IsActive"));
        Assert.Empty(fixture.WriteInvocations);
        fixture.AssertLookupNotification();
    }

    [Theory]
    [MemberData(nameof(LookupCases))]
    public async Task PersistenceException_IsNotConvertedToSuccess(string resource, string operation)
    {
        var fixture = new TransitionFixture(Find(resource)) { PersistenceFailure = new IOException("Test persistence failure") };
        await Assert.ThrowsAsync<IOException>(() => fixture.CallApp(operation));
        Assert.Single(fixture.WriteInvocations);
    }

    [Theory]
    [MemberData(nameof(IdentityCases))]
    public async Task ExistingIdentityRules_ArePreserved(string resource, string operation)
    {
        var fixture = new TransitionFixture(Find(resource));
        fixture.SetProperty("Id", 0);
        Assert.Contains((await fixture.Validate(operation)).Errors, error => error.PropertyName == "Id");
    }

    [Theory]
    [MemberData(nameof(DeletedGuardCases))]
    public async Task ExistingDeletedGuards_ArePreserved(string resource, string operation)
    {
        var fixture = new TransitionFixture(Find(resource));
        fixture.Mutate("Delete");
        Assert.Contains((await fixture.Validate(operation)).Errors, error => error.PropertyName == "IsDeleted");
    }

    [Theory]
    [MemberData(nameof(AuditGuardCases))]
    public async Task ExistingModifiedByRules_ArePreserved(string resource, string operation)
    {
        var fixture = new TransitionFixture(Find(resource));
        fixture.SetProperty("ModifiedBy", 0);
        Assert.Contains((await fixture.Validate(operation)).Errors, error => error.PropertyName == "ModifiedBy");
    }

    [Theory]
    [MemberData(nameof(ParentGuardCases))]
    public async Task ExistingTenantAndParentGuards_ArePreserved(string resource, string operation, string property)
    {
        var fixture = new TransitionFixture(Find(resource));
        fixture.SetProperty(property, 2);
        Assert.False(await fixture.CallApp(operation));
        Assert.Empty(fixture.WriteInvocations);
    }

    [Theory]
    [InlineData("Activate")]
    [InlineData("Deactivate")]
    public async Task ClientAddress_StateChangeRetainsAddressValidation(string operation)
    {
        var fixture = new TransitionFixture(Find("ClientAddresses"));
        fixture.SetProperty("Street", "");
        Assert.False(await fixture.CallApp(operation));
        Assert.Empty(fixture.WriteInvocations);
        fixture.Notify.Verify(notify => notify.Add("Domain.ClientAddress.StreetRequired", 400), Times.Once);
    }

    [Fact]
    public async Task ClientAddress_UpdateStillRejectsDeletedResource()
    {
        var fixture = new TransitionFixture(Find("ClientAddresses"));
        fixture.Mutate("Delete");
        Assert.Contains((await fixture.Validate("Update")).Errors,
            error => error.ErrorMessage == "Domain.ClientAddress.CannotUpdateDeleted");
    }

    [Fact]
    public async Task JobDefinition_StillRequiresCronAndProtectsSystemJobs()
    {
        var localization = new Mock<ILocalizationService>();
        localization.Setup(service => service.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        localization.Setup(service => service.GetMessage(It.IsAny<string>(), It.IsAny<object?[]>()))
            .Returns((string key, object?[] arguments) => key);
        var validator = new Validators.Job.JobDefinitionValidator(localization.Object);
        var job = new Entities.Job.JobDefinitionsEntity("security", "test",
            typeof(VianaHub.Global.Gerit.Infra.Job.Jobs.Security.JwtKeyRotationJob).AssemblyQualifiedName,
            1, cronExpression: null, isSystemJob: true);

        Assert.Contains((await validator.ValidateForActivateAsync(job)).Errors,
            error => error.ErrorMessage == "Domain.Job.CronRequiredForRecurring");
        Assert.Contains((await validator.ValidateForDeleteAsync(job)).Errors,
            error => error.ErrorMessage == "Domain.Job.CannotDeleteSystemJob");
    }

    [Fact]
    public async Task Subscription_TogglesDoNotChangeFinancialOrCancellationState()
    {
        var fixture = new TransitionFixture(Find("Subscription"));
        var start = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        fixture.SetProperty("AgreedAmount", 123m);
        fixture.SetProperty("StatusDefinitionId", 3);
        fixture.SetProperty("StatusDomainId", 4);
        fixture.SetProperty("CurrentPeriodStart", start);
        fixture.SetProperty("CurrentPeriodEnd", start.AddMonths(1));

        foreach (var operation in new[] { "Deactivate", "Deactivate", "Activate", "Activate" })
            Assert.True(await fixture.CallApp(operation));

        Assert.Equal(123m, fixture.GetProperty<decimal>("AgreedAmount"));
        Assert.Equal(3, fixture.GetProperty<int>("StatusDefinitionId"));
        Assert.Equal(4, fixture.GetProperty<int>("StatusDomainId"));
        Assert.Equal(start, fixture.GetProperty<DateTime>("CurrentPeriodStart"));
        Assert.Equal(start.AddMonths(1), fixture.GetProperty<DateTime>("CurrentPeriodEnd"));
        Assert.False(fixture.GetProperty<bool>("CancelAtPeriodEnd"));
        Assert.Null(fixture.GetProperty<DateTime?>("CanceledAt"));
    }

    private static ResourceCase Find(string name) => Resources.Single(resource => resource.Name == name);

    private sealed record ResourceCase(string Name, Type EntityType, Type ValidatorType, Type? AppType, Type? DomainType, int LookupStatus = 410);

    private sealed class TransitionFixture
    {
        private readonly ResourceCase _resource;
        private readonly Dictionary<Type, object> _services = new();
        private readonly List<Mock> _repositories = [];
        private readonly object _entity;
        public Mock<INotify> Notify { get; } = new();
        public bool MissingLookup { get; init; }
        public Exception? PersistenceFailure { get; init; }
        public IEnumerable<IInvocation> WriteInvocations => _repositories.SelectMany(repository => repository.Invocations)
            .Where(invocation => invocation.Method.Name == "UpdateAsync");

        public TransitionFixture(ResourceCase resource)
        {
            _resource = resource;
            _entity = resource.EntityType == typeof(Entities.Business.ClientAddressesEntity)
                ? new Entities.Business.ClientAddressesEntity(1, 1, 1, "PT", "Rua de Teste", "1", null, null, "Lisboa", null, "1000-001", null, null, null, false, 1)
                : Activator.CreateInstance(resource.EntityType, nonPublic: true)!;
            foreach (var name in new[] { "Id", "TenantId", "ClientId", "EmployeeId", "VisitId", "VisitTeamId", "CreatedBy", "ModifiedBy" })
                SetProperty(name, 1);

            var localization = new Mock<ILocalizationService>();
            localization.Setup(service => service.GetMessage(It.IsAny<string>())).Returns((string key) => key);
            localization.Setup(service => service.GetMessage(It.IsAny<string>(), It.IsAny<object?[]>()))
                .Returns((string key, object?[] arguments) => key);
            var user = new Mock<ICurrentUserService>();
            user.Setup(service => service.GetUserId()).Returns(1);
            user.Setup(service => service.GetTenantId()).Returns(1);
            _services[typeof(ILocalizationService)] = localization.Object;
            _services[typeof(ICurrentUserService)] = user.Object;
            _services[typeof(INotify)] = Notify.Object;
            Mutate("Activate");
        }

        public void SetProperty(string name, object value)
        {
            foreach (var property in _resource.EntityType.GetProperties().Where(property => property.Name == name && property.SetMethod is not null))
            {
                var target = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                property.SetValue(_entity, Convert.ChangeType(value, target));
            }
        }

        public T GetProperty<T>(string name) => (T)_resource.EntityType.GetProperty(name)!.GetValue(_entity)!;

        public void Mutate(string operation)
        {
            var method = _resource.EntityType.GetMethod(operation)!;
            method.Invoke(_entity, method.GetParameters().Select(_ => (object)1).ToArray());
        }

        public Task<ValidationResult> Validate(string operation)
        {
            var validator = Resolve(_resource.ValidatorType);
            return (Task<ValidationResult>)validator.GetType().GetMethod("ValidateFor" + operation + "Async")!.Invoke(validator, [_entity])!;
        }

        public Task<bool> CallDomain(string operation)
        {
            var domain = Resolve(_resource.DomainType!);
            return (Task<bool>)domain.GetType().GetMethod(operation + "Async")!.Invoke(domain, [_entity, CancellationToken.None])!;
        }

        public Task<bool> CallApp(string operation)
        {
            var app = Resolve(_resource.AppType!);
            var method = app.GetType().GetMethod(operation + "Async")!;
            var arguments = method.GetParameters().Select(parameter => parameter.ParameterType == typeof(CancellationToken)
                ? (object)CancellationToken.None : Convert.ChangeType(1, Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType)).ToArray();
            return (Task<bool>)method.Invoke(app, arguments)!;
        }

        public void AssertLookupNotification() =>
            Notify.Verify(notify => notify.Add(It.IsAny<string>(), _resource.LookupStatus), Times.Once);

        private object Resolve(Type type)
        {
            if (_services.TryGetValue(type, out var service)) return service;
            if (type.IsAssignableFrom(_resource.ValidatorType) && type != typeof(object))
            {
                if (type != _resource.ValidatorType) return Resolve(_resource.ValidatorType);
            }
            else if (_resource.DomainType is not null && type.IsAssignableFrom(_resource.DomainType) && type != typeof(object))
            {
                if (type != _resource.DomainType) return Resolve(_resource.DomainType);
            }

            if (type.IsInterface)
            {
                var mock = (Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(type))!;
                if (type.Name.EndsWith("Repository", StringComparison.Ordinal))
                {
                    mock.DefaultValueProvider = new RepositoryDefaults(this);
                    _repositories.Add(mock);
                }
                service = mock.Object;
            }
            else
            {
                var constructor = type.GetConstructors().Single();
                service = constructor.Invoke(constructor.GetParameters().Select(parameter => Resolve(parameter.ParameterType)).ToArray());
            }
            _services[type] = service;
            return service;
        }

        private sealed class RepositoryDefaults(TransitionFixture fixture) : DefaultValueProvider
        {
            protected override object GetDefaultValue(Type type, Mock mock)
            {
                if (type == typeof(Task<bool>)) return fixture.PersistenceFailure is null
                    ? Task.FromResult(true) : Task.FromException<bool>(fixture.PersistenceFailure);
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>) && type.GenericTypeArguments[0] == fixture._resource.EntityType)
                {
                    // Simula somente a filtragem de soft delete já existente no lookup; persistência é observada separadamente.
                    var entity = fixture.MissingLookup || fixture.GetProperty<bool>("IsDeleted") ? null : fixture._entity;
                    return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(fixture._resource.EntityType).Invoke(null, [entity])!;
                }
                return type.IsValueType ? Activator.CreateInstance(type)! : null!;
            }
        }
    }
}
