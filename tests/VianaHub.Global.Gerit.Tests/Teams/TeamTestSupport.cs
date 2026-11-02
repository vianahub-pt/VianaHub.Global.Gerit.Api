using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VianaHub.Global.Gerit.Application.Mappings.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Tests.Teams;

internal static class TeamTestSupport
{
    internal static IMapper Mapper() => new MapperConfiguration(
        config => config.AddProfile<TeamMappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    internal static Mock<ILocalizationService> Localization()
    {
        var mock = new Mock<ILocalizationService>();
        mock.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        mock.Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object?[]>()))
            .Returns((string key, object?[] _) => key);
        return mock;
    }

    internal static TeamEntity Entity(int id = 42, int tenantId = 7, string name = "Crew")
    {
        var entity = new TeamEntity(tenantId, name, "Description", 17);
        typeof(TeamEntity).GetProperty(nameof(TeamEntity.Id))!.SetValue(entity, id);
        return entity;
    }
}

// Injeta uma gravação sem linhas afetadas sem substituir AppService, domínio ou repository.
internal sealed class TeamSaveControl : SaveChangesInterceptor
{
    public bool Suppress { get; set; }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Suppress ? InterceptionResult<int>.SuppressWithResult(0) : result);
}
