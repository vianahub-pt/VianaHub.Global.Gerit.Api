using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Application.Services.Identity;
using System.Text;

namespace VianaHub.Global.Gerit.Tests.RolePermissions;

public sealed class RolePermissionBulkBoundaryTests
{
    [Fact]
    public void CsvParserAcceptsExactlyTenThousandRows()
    {
        using var stream = BuildCsv(10_000);
        var result = RolePermissionBulkCsvParser.Parse(stream, DomainExtensions.GetMaxCsvRows());

        Assert.True(result.IsSuccess);
        Assert.Equal(10_000, result.Items!.Count);
    }

    [Fact]
    public void CsvParserRejectsTheRowAfterTenThousand()
    {
        using var stream = BuildCsv(10_001);
        var result = RolePermissionBulkCsvParser.Parse(stream, DomainExtensions.GetMaxCsvRows());

        Assert.False(result.IsSuccess);
        Assert.Equal("ReadCsvFile.MaxRows", result.ErrorKey);
    }

    private static MemoryStream BuildCsv(int rows)
    {
        var builder = new StringBuilder("RoleId;ResourceId;ActionId\n");
        for (var index = 0; index < rows; index++)
            builder.Append("1;1;").Append(index + 1).Append('\n');

        return new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString()));
    }
}
