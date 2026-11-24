using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Tests.IdentityMappings;

public sealed class IsActiveCatalogEntityTests
{
    [Fact] public void AddressType_HonorsCreateAndUpdateState() => AssertState(new AddressTypeEntity("HOME", 1, false), new AddressTypeEntity("HOME", 1), e => e.MarkModified(2), e => e.MarkModified(2, true));
    [Fact] public void AcquisitionSourceType_HonorsCreateAndUpdateState() => AssertState(new AcquisitionSourceTypeEntity("SOURCE", 1, false), new AcquisitionSourceTypeEntity("SOURCE", 1), e => e.MarkModified(2), e => e.MarkModified(2, true));
    [Fact] public void DocumentType_HonorsCreateAndUpdateState() => AssertState(new DocumentTypeEntity("ID", 1, false), new DocumentTypeEntity("ID", 1), e => e.MarkModified(2), e => e.MarkModified(2, true));
    [Fact] public void PartyType_HonorsCreateAndUpdateState() => AssertState(new PartyTypeEntity("PERSON", 1, false), new PartyTypeEntity("PERSON", 1), e => e.MarkModified(2), e => e.MarkModified(2, true));
    [Fact] public void StatusDomain_HonorsCreateAndUpdateState() => AssertState(new StatusDomainEntity("VISIT", 1, false), new StatusDomainEntity("VISIT", 1), e => e.MarkModified(2), e => e.MarkModified(2, true));

    [Fact]
    public void FileType_HonorsCreateAndUpdateState()
    {
        var defaults = new FileTypeEntity("application/pdf", ".pdf", "PDF", 1);
        var entity = new FileTypeEntity("application/pdf", ".pdf", "PDF-INACTIVE", 1, false);
        Assert.True(defaults.IsActive);
        Assert.False(entity.IsActive);
        entity.Update("application/octet-stream", ".bin", 2);
        Assert.False(entity.IsActive);
        entity.Update("application/pdf", ".pdf", 2, true);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void StatusDefinition_HonorsCreateAndUpdateState()
    {
        var defaults = new StatusDefinitionEntity(7, 1, "PENDING", 1, false, 1);
        var entity = new StatusDefinitionEntity(7, 1, "INACTIVE", 2, false, 1, false);
        Assert.True(defaults.IsActive);
        Assert.False(entity.IsActive);
        entity.Update(3, false, 2);
        Assert.False(entity.IsActive);
        entity.Update(4, false, 2, true);
        Assert.True(entity.IsActive);
    }

    private static void AssertState<TEntity>(TEntity entity, TEntity defaults, Action<TEntity> omittedUpdate, Action<TEntity> activeUpdate)
        where TEntity : class
    {
        var active = (bool)typeof(TEntity).GetProperty("IsActive")!.GetValue(entity)!;
        Assert.False(active);
        omittedUpdate(entity);
        Assert.False((bool)typeof(TEntity).GetProperty("IsActive")!.GetValue(entity)!);
        activeUpdate(entity);
        Assert.True((bool)typeof(TEntity).GetProperty("IsActive")!.GetValue(entity)!);
        Assert.True((bool)typeof(TEntity).GetProperty("IsActive")!.GetValue(defaults)!);
    }
}
