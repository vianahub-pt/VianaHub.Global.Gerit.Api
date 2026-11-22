using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Tests.UserPreferences;

internal static class UserPreferencesTestSupport
{
    internal static Mock<ILocalizationService> Localization()
    {
        var mock = new Mock<ILocalizationService>();
        mock.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        mock.Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object?[]>()))
            .Returns((string key, object?[] _) => key);
        return mock;
    }

    internal static UserPreferencesEntity Entity(int id = 42, int tenantId = 7, string name = "Crew")
    {
        var entity = new UserPreferencesEntity(tenantId, 17, "light", "EUR", "pt-PT", "Europe/Lisbon", "dd/MM/yyyy", "24h", TimeSpan.FromHours(9), TimeSpan.FromHours(18), 17);
        typeof(UserPreferencesEntity).GetProperty(nameof(UserPreferencesEntity.Id))!.SetValue(entity, id);
        return entity;
    }
}

// Injeta uma gravação sem linhas afetadas sem substituir AppService, domínio ou repository.
internal sealed class UserPreferencesSaveControl : SaveChangesInterceptor
{
    public bool Suppress { get; set; }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Suppress ? InterceptionResult<int>.SuppressWithResult(0) : result);
}
