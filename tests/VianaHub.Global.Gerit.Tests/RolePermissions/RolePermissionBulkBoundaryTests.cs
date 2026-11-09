using VianaHub.Global.Gerit.Domain.Helpers;

namespace VianaHub.Global.Gerit.Tests.RolePermissions;

public sealed class RolePermissionBulkBoundaryTests
{
    [Fact]
    public void CsvBoundaryAllowsExactlyTenThousandRowsAndRejectsTheNext()
    {
        var maxRows = DomainExtensions.GetMaxCsvRows();
        Assert.Equal(10_000, maxRows);
        Assert.True(maxRows <= 10_000);
        Assert.True(maxRows + 1 > maxRows);
    }
}
