using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Api.Services;

namespace VianaHub.Global.Gerit.Tests;

[CollectionDefinition("LocalizationService thread safety", DisableParallelization = true)]
public sealed class LocalizationServiceThreadSafetyCollection;

[Collection("LocalizationService thread safety")]
public class LocalizationServiceThreadSafetyTests
{
    private const string ValidationTitleKey = "Api.Helpers.NotifyExtensions.Error.ValidationError";

    [Fact]
    public async Task EmptyCache_ConcurrentSameCulture_ReturnsDeterministicMessages()
    {
        ClearCache();
        var service = CreateService("en-US");
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 128).Select(async _ =>
        {
            await gate.Task;
            return service.GetMessage(ValidationTitleKey);
        }).ToArray();

        gate.SetResult();
        var messages = await Task.WhenAll(tasks);

        Assert.All(messages, message => Assert.Equal("Validation Error", message));
    }

    [Fact]
    public async Task EmptyCache_ConcurrentSupportedCultures_ReturnsCultureSpecificMessages()
    {
        ClearCache();
        var expectations = new Dictionary<string, string>
        {
            ["pt-PT"] = "Erro de validação",
            ["pt-BR"] = "Erro de validação",
            ["en-US"] = "Validation Error",
            ["es-ES"] = "Error de validación"
        };
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = expectations.SelectMany(pair => Enumerable.Range(0, 32).Select(async _ =>
        {
            await gate.Task;
            return (pair.Key, Message: CreateService(pair.Key).GetMessage(ValidationTitleKey));
        })).ToArray();

        gate.SetResult();
        var results = await Task.WhenAll(tasks);

        Assert.All(results, result => Assert.Equal(expectations[result.Key], result.Message));
    }

    [Fact]
    public async Task PopulatedCache_ConcurrentReads_PreserveFormattingFallbackAndRawKey()
    {
        ClearCache();
        var service = CreateService("en-US");
        var formatKey = "Api.Validator.Client.Create.Note.MaximumLength";
        var template = service.GetMessage(formatKey);
        Assert.Contains("{0}", template, StringComparison.Ordinal);

        var formattedTasks = Enumerable.Range(0, 128)
            .Select(_ => Task.Run(() => service.GetMessage(formatKey, 25)));
        var formattedMessages = await Task.WhenAll(formattedTasks);

        Assert.All(formattedMessages, message => Assert.Equal(string.Format(template, 25), message));
        Assert.Equal("Chave.Inexistente", service.GetMessage("Chave.Inexistente"));
        Assert.Equal("Erro de validação", CreateService("fr-FR").GetMessage(ValidationTitleKey));
    }

    [Fact]
    public void MissingRequestCulture_UsesCurrentUiCultureWithoutChangingContract()
    {
        ClearCache();
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pt-PT");
            var service = new LocalizationService(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

            Assert.Equal("pt-PT", service.GetCurrentCulture());
            Assert.Equal("Erro de validação", service.GetMessage(ValidationTitleKey));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    private static LocalizationService CreateService(string culture)
    {
        var context = new DefaultHttpContext();
        context.Items["RequestCulture"] = culture;
        return new LocalizationService(new HttpContextAccessor { HttpContext = context });
    }

    private static void ClearCache()
    {
        var cache = typeof(LocalizationService)
            .GetField("_cache", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
        cache.GetType().GetMethod("Clear")!.Invoke(cache, null);
    }
}
