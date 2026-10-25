using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace VianaHub.Global.Gerit.Api.ModelBinders;

/// <summary>
/// Provider para o GuidNoDashesModelBinder.
/// Registra o model binder customizado para todos os parâmetros do tipo Guid.
/// </summary>
public class GuidNoDashesModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.Metadata.ModelType == typeof(Guid) || context.Metadata.ModelType == typeof(Guid?))
        {
            return new GuidNoDashesModelBinder();
        }

        return null;
    }
}
