namespace VianaHub.Global.Gerit.Api.Helpers;

/// <summary>
/// Classe auxiliar para armazenar o ServiceProvider globalmente
/// </summary>
public static class ServiceProviderHolder
{
    public static IServiceProvider? ServiceProvider { get; set; }
}
