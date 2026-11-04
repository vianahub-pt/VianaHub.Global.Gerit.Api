using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.UserPreferences;

namespace VianaHub.Global.Gerit.Tests.UserPreferences;

public class UserPreferencesMappingTests
{
    [Fact]
    public void Profile_IsValid()
    {
        UserPreferencesTestSupport.Mapper().ConfigurationProvider.AssertConfigurationIsValid();
    }

    [Fact]
    public void DetailMapping_PreservesTimesAndEmailFlags()
    {
        var entity = UserPreferencesTestSupport.Entity();
        entity.Update(
            "dark", "en-US", "USD", "Europe/Lisbon", "yyyy-MM-dd", "12h",
            new TimeSpan(22, 15, 0), new TimeSpan(6, 30, 0),
            true, true, false, true, false, true, 17);

        var response = UserPreferencesTestSupport.Mapper().Map<UserPreferencesDetailResponse>(entity);

        Assert.Equal("22:15", response.DayStart);
        Assert.Equal("06:30", response.DayEnd);
        Assert.True(response.EmailNewsletter);
        Assert.True(response.EmailWeeklyReport);
        Assert.False(response.EmailApproval);
        Assert.True(response.EmailAlerts);
        Assert.False(response.EmailReminders);
        Assert.True(response.EmailPlanner);
    }
}
