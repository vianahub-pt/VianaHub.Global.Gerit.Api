using Xunit;

namespace VianaHub.Global.Gerit.Tests.Infrastructure;

public sealed class SqlServerIntegrationTheoryAttribute : TheoryAttribute
{
    public SqlServerIntegrationTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING")))
            Skip = "SQL-backed integration requires GERIT_RLS_TEST_CONNECTIONSTRING.";
    }
}
