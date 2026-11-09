using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.RolePermission;
using VianaHub.Global.Gerit.Domain.Helpers;

namespace VianaHub.Global.Gerit.Application.Services.Identity;

internal sealed record RolePermissionCsvParseResult(
    List<BulkUploadRolePermissionItem>? Items,
    string? ErrorKey = null,
    object? ErrorArgument = null)
{
    public bool IsSuccess => Items is not null;
}

internal static class RolePermissionBulkCsvParser
{
    internal static RolePermissionCsvParseResult Parse(Stream stream, int maxRows)
    {
        try
        {
            using var reader = stream.CreateUtf8StreamReader();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ";",
                TrimOptions = TrimOptions.Trim
            };

            using var csv = new CsvReader(reader, config);
            if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord is null)
                return new(null, "BulkUpload.EmptyFile");

            var expectedHeaders = new[] { "RoleId", "ResourceId", "ActionId" };
            if (csv.HeaderRecord.Length != expectedHeaders.Length ||
                !expectedHeaders.SequenceEqual(csv.HeaderRecord, StringComparer.OrdinalIgnoreCase))
            {
                return new(null, "ReadCsvFile.InvalidHeader");
            }

            var records = new List<BulkUploadRolePermissionItem>();
            var rowCount = 0;
            while (csv.Read())
            {
                if (rowCount >= maxRows)
                    return new(null, "ReadCsvFile.MaxRows", maxRows);

                var record = csv.GetRecord<BulkUploadRolePermissionItem>();
                if (record is null)
                    return new(null, "ReadCsvFile.Exception");

                if (record.RoleId <= 0)
                    return new(null, "ValidateBulkItem.RoleId", rowCount + 2);
                if (record.ResourceId <= 0)
                    return new(null, "ValidateBulkItem.ResourceId", rowCount + 2);
                if (record.ActionId <= 0)
                    return new(null, "ValidateBulkItem.ActionId", rowCount + 2);

                records.Add(record);
                rowCount++;
            }

            return new(records);
        }
        catch (CsvHelperException)
        {
            return new(null, "ReadCsvFile.Exception");
        }
        catch (InvalidDataException)
        {
            return new(null, "ReadCsvFile.Exception");
        }
        catch (IOException)
        {
            return new(null, "ReadCsvFile.Exception");
        }
        catch (DecoderFallbackException)
        {
            return new(null, "ReadCsvFile.Exception");
        }
    }
}
