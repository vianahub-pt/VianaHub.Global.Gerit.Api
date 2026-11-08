using VianaHub.Global.Gerit.Domain.Helpers;

namespace VianaHub.Global.Gerit.Tests.Roles;

public sealed class RoleBulkBoundaryTests
{
    [Fact]
    public void CsvRowLimitAccepts10000AndRejects10001()
    {
        var maxRows = DomainExtensions.GetMaxCsvRows();

        Assert.Equal(10_000, maxRows);
        Assert.True(maxRows.IsValidCsvRowCount());
        Assert.False((maxRows + 1).IsValidCsvRowCount());
    }
}
