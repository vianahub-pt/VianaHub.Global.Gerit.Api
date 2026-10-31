using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using VianaHub.Global.Gerit.Api;

namespace VianaHub.Global.Gerit.Tests;

public class DatabaseStartupArchitectureTests
{
    [Fact]
    public void RuntimeAssemblies_DoNotContainDatabaseBootstrapOrEfProvisioningCalls()
    {
        // Inspecionar o binário cobre também helpers e hosted services, sem depender da formatação do startup.
        var directory = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
        var assemblies = Directory.GetFiles(directory, "VianaHub.Global.Gerit.*.dll")
            .Where(path => !Path.GetFileNameWithoutExtension(path).EndsWith(".Tests", StringComparison.Ordinal))
            .ToArray();
        Assert.Contains(typeof(Program).Assembly.Location, assemblies);
        Assert.Contains(assemblies, path => Path.GetFileName(path) == "VianaHub.Global.Gerit.Infra.Data.dll");

        var violations = new List<string>();
        foreach (var path in assemblies)
        {
            using var stream = File.OpenRead(path);
            using var portableExecutable = new PEReader(stream);
            var metadata = portableExecutable.GetMetadataReader();
            foreach (var handle in metadata.MethodDefinitions)
            {
                var name = metadata.GetString(metadata.GetMethodDefinition(handle).Name);
                if (name is "InitializeDatabaseAsync" or "MigrateDatabaseAsync" or "SeedDatabaseAsync")
                    violations.Add($"{Path.GetFileName(path)}: {name}");
            }
            foreach (var handle in metadata.TypeDefinitions)
            {
                if (metadata.GetString(metadata.GetTypeDefinition(handle).Name) == "DatabaseSeeder")
                    violations.Add($"{Path.GetFileName(path)}: DatabaseSeeder");
            }
            foreach (var handle in metadata.MemberReferences)
            {
                var member = metadata.GetMemberReference(handle);
                var name = metadata.GetString(member.Name);
                if (name is not ("Migrate" or "MigrateAsync" or "EnsureCreated" or "EnsureCreatedAsync") ||
                    member.Parent.Kind != HandleKind.TypeReference)
                    continue;
                var type = metadata.GetTypeReference((TypeReferenceHandle)member.Parent);
                if (metadata.GetString(type.Namespace).StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
                    violations.Add($"{Path.GetFileName(path)}: {metadata.GetString(type.Name)}.{name}");
            }
        }
        Assert.Empty(violations);
    }

    [Fact]
    public void ApiConfiguration_DoesNotExposeDatabaseStartupMutationFlags()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "VianaHub.Global.Gerit.sln")))
            root = root.Parent;
        Assert.NotNull(root);
        var files = Directory.GetFiles(Path.Combine(root.FullName, "src", "VianaHub.Global.Gerit.Api"), "appsettings*.json");
        Assert.NotEmpty(files);
        foreach (var path in files)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
            if (!document.RootElement.TryGetProperty("Database", out var database)) continue;
            Assert.DoesNotContain(database.EnumerateObject(), property => property.Name is "AutoMigrate" or "AutoSeed" or "AutoSchemaReconcile");
        }
    }
}
