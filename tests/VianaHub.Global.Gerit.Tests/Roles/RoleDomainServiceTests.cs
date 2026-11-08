using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Moq;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Services.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Tests.Roles;

public sealed class RoleDomainServiceTests
{
    [Fact]
    public async Task UnrelatedDatabaseErrorsAreNotConvertedToRoleConflicts()
    {
        var repository = new Mock<IRoleDataRepository>();
        var validator = new Mock<IEntityDomainValidator<RoleEntity>>();
        validator.Setup(x => x.ValidateForCreateAsync(It.IsAny<RoleEntity>())).ReturnsAsync(new ValidationResult());
        var databaseError = new DbUpdateException("foreign key failure");
        repository.Setup(x => x.AddAsync(It.IsAny<RoleEntity>(), It.IsAny<CancellationToken>())).ThrowsAsync(databaseError);
        var service = new RoleDomainService(repository.Object, validator.Object, new Notify());

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => service.CreateAsync(new RoleEntity(1, "TEST_ROLE_DB_ERROR", "Description", 1), default));

        Assert.Same(databaseError, exception);
    }
}
