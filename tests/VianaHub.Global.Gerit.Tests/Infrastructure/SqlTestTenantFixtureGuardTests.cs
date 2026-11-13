namespace VianaHub.Global.Gerit.Tests.Infrastructure;

public sealed class SqlTestTenantFixtureGuardTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public async Task DeleteAsync_RejectsUnownedIdBeforeConstructingConnection(int tenantId)
    {
        // String inválida prova que a guarda ocorre antes de qualquer acesso SQL.
        var fixture = new SqlTestTenantFixture("Encrypt=true;not a connection string");
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.DeleteAsync(tenantId));
        Assert.Equal("Only tenants created by this fixture instance can be deleted.", error.Message);
    }
}
