using VianaHub.Global.Gerit.Application.Mappings.Job;
using VianaHub.Global.Gerit.Domain.Entities.Job;

namespace VianaHub.Global.Gerit.Tests.Job;

public class JobMapperTests
{
    [Fact]
    public void ToDetail_MapsEntityFieldsAndPreservesUnmappedExecutionDefaults()
    {
        var entity = new JobDefinitionsEntity(
            "Maintenance", "Cleanup", "Recurring", 7,
            description: "Remove expired records", jobPurpose: "Housekeeping",
            jobMethod: "Run", cronExpression: "0 0 * * *", timeZoneId: "UTC",
            executeOnlyOnce: false, timeoutMinutes: 12, priority: 3,
            queue: "maintenance", maxRetries: 4, jobConfiguration: "{}", isSystemJob: true);

        var result = JobMapper.ToDetail(entity);

        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(entity.JobName, result.JobName);
        Assert.Equal(entity.Description, result.Description);
        Assert.Equal(entity.CreatedBy, result.CreatedBy);
        Assert.Equal(entity.CreatedAt, result.CreatedAt);
        Assert.Equal(entity.ModifiedAt, result.UpdatedAt);
        Assert.Equal(0, result.Status);
        Assert.Null(result.NextExecution);
        Assert.Null(result.LastExecution);
        Assert.Null(result.LastExecutionStatus);
    }
}
