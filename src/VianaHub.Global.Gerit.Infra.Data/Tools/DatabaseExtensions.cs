using VianaHub.Global.Gerit.Domain.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.RegularExpressions;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Seeders;

namespace VianaHub.Global.Gerit.Infra.Data.Tools;

/// <summary>
/// Extensões para configuração e inicialização do banco de dados
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Aplica migrations automaticamente (apenas Dev/Staging)
    /// </summary>
    public static async Task MigrateDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<GeritDbContext>>();

        try
        {
            logger.LogInformation("?? Verificando estado do banco de dados...");

            var context = services.GetRequiredService<GeritDbContext>();

            // Verifica se o banco existe
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogInformation("?? Banco de dados não existe, criando...");
                await context.Database.MigrateAsync();
                logger.LogInformation("? Banco de dados criado com sucesso");
                return;
            }

            // Verifica migrations aplicadas
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

            logger.LogInformation("??  Migrations aplicadas: {Count}", appliedMigrations.Count());

            if (pendingMigrations.Any())
            {
                logger.LogWarning("??  Migrations pendentes encontradas: {Migrations}",
                    string.Join(", ", pendingMigrations));

                logger.LogInformation("?? Aplicando migrations pendentes...");
                await context.Database.MigrateAsync();
                logger.LogInformation("? Migrations aplicadas com sucesso");
            }
            else
            {
                logger.LogInformation("?  Banco de dados está atualizado, nenhuma migration pendente");
            }

            // --- Validação e aplicação do script de criação de tabelas/indexes/RLS ---
            var env = services.GetService<IHostEnvironment>();
            var contentRoot = env?.ContentRootPath ?? AppContext.BaseDirectory;
            var scriptPath = Path.Combine(contentRoot, "docs", "sql", "Create-Tables.sql");

            if (!File.Exists(scriptPath))
            {
                logger.LogWarning("?? Create-Tables.sql não encontrado em: {Path}", scriptPath);
                return;
            }

            var script = await File.ReadAllTextAsync(scriptPath);

            // Split by GO (line with only GO)
            var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                              .Select(b => b.Trim())
                              .Where(b => !string.IsNullOrWhiteSpace(b))
                              .ToList();

            var connection = context.Database.GetDbConnection();
            await using (connection)
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                // Collect CREATE TABLE batches and others
                var tableBatches = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var functionBatches = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var indexBatches = new List<(string Name, string Table, string Sql)>();
                var securityPolicyBatches = new List<string>();

                foreach (var batch in batches)
                {
                    var batchUpper = batch.ToUpperInvariant();
                    if (batchUpper.Contains("CREATE TABLE"))
                    {
                        // attempt to extract table name with regex
                        var m = Regex.Match(batch, @"CREATE\s+TABLE\s+(?:\[?(?<schema>\w+)\]?\.)?\[?(?<table>\w+)\]?", RegexOptions.IgnoreCase);
                        if (m.Success)
                        {
                            var table = m.Groups["table"].Value;
                            tableBatches[table] = batch;
                            continue;
                        }
                    }

                    if (batchUpper.Contains("CREATE FUNCTION"))
                    {
                        var m = Regex.Match(batch, @"CREATE\s+FUNCTION\s+(?:\[?(?<schema>\w+)\]?\.)?\[?(?<name>\w+)\]?", RegexOptions.IgnoreCase);
                        if (m.Success)
                        {
                            var fnName = m.Groups["name"].Value;
                            functionBatches[fnName] = batch;
                            continue;
                        }
                    }

                    if (batchUpper.Contains("CREATE INDEX") || batchUpper.Contains("CREATE UNIQUE INDEX") || batchUpper.Contains("CREATE NONCLUSTERED INDEX"))
                    {
                        // Try to extract index name and table
                        var m = Regex.Match(batch, @"CREATE\s+(?:UNIQUE\s+)?(?:NONCLUSTERED\s+)?INDEX\s+\[?(?<index>\w+)\]?\s+ON\s+(?:\[?(?<schema>\w+)\]?\.)?\[?(?<table>\w+)\]?", RegexOptions.IgnoreCase);
                        if (m.Success)
                        {
                            var idxName = m.Groups["index"].Value;
                            var table = m.Groups["table"].Value;
                            indexBatches.Add((idxName, table, batch));
                            continue;
                        }
                        else
                        {
                            // fallback: store whole batch for execution if missing check fails
                            indexBatches.Add((string.Empty, string.Empty, batch));
                        }
                    }

                    if (batchUpper.Contains("CREATE SECURITY POLICY") || batchUpper.Contains("ADD FILTER PREDICATE") || batchUpper.Contains("CREATE SECURITY POLICY"))
                    {
                        securityPolicyBatches.Add(batch);
                        continue;
                    }
                }

                // Helper to execute sql safely
                async Task ExecuteSafeAsync(string sql, string description)
                {
                    try
                    {
                        await using var cmd = connection.CreateCommand();
                        cmd.CommandText = sql;
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandTimeout = 60;
                        await cmd.ExecuteNonQueryAsync();
                        logger.LogInformation("? {Description} aplicado com sucesso", description);
                    }
                    catch (Exception ex)
                    {
                        // Log and continue; many CREATE statements will fail if objects already exist
                        logger.LogWarning("?? Falha ao aplicar {Description}: {Message} | Exception: {ExceptionDetails}", description, LogValueSanitizer.Sanitize(ex.Message), LogValueSanitizer.ExceptionDetails(ex));
                    }
                }

                // 1) Create missing tables
                foreach (var kv in tableBatches)
                {
                    var tableName = kv.Key;
                    var batchSql = kv.Value;

                    // check existence
                    await using (var checkCmd = connection.CreateCommand())
                    {
                        checkCmd.CommandText = "SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = @name AND s.name = 'dbo'";
                        var p = checkCmd.CreateParameter(); p.ParameterName = "@name"; p.Value = tableName; checkCmd.Parameters.Add(p);
                        var exists = (int)(await checkCmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("COUNT query did not return a value.")) > 0;
                        if (!exists)
                        {
                            logger.LogInformation("?? Tabela {Table} ausente. Criando...", tableName);
                            await ExecuteSafeAsync(batchSql, $"CREATE TABLE {tableName}");
                        }
                        else
                        {
                            // Table exists: ensure columns present
                            // Parse column definitions from batch (rudimentary: lines until a CONSTRAINT or closing)
                            var cols = new List<(string Name, string Definition)>();
                            // extract content between first '(' after CREATE TABLE and final ')' before end
                            var idx = batchSql.IndexOf('(');
                            var lastIdx = batchSql.LastIndexOf(')');
                            if (idx > -1 && lastIdx > idx)
                            {
                                var inner = batchSql.Substring(idx + 1, lastIdx - idx - 1);
                                // split by commas but avoid splitting inside parentheses by a simple approach
                                var parts = Regex.Split(inner, @",\r?\n(?=\s*\[?\w+\]?\s|\s*\w+\s)");
                                foreach (var part in parts)
                                {
                                    var line = part.Trim();
                                    if (string.IsNullOrWhiteSpace(line)) continue;
                                    // stop at CONSTRAINT / PRIMARY / UNIQUE / FOREIGN
                                    var up = line.ToUpperInvariant();
                                    if (up.StartsWith("CONSTRAINT") || up.StartsWith("PRIMARY KEY") || up.StartsWith("UNIQUE") || up.StartsWith("FOREIGN KEY")) continue;

                                    var m = Regex.Match(line, @"^\[?(?<col>\w+)\]?\s+(?<def>.+)$", RegexOptions.Singleline);
                                    if (m.Success)
                                    {
                                        var col = m.Groups["col"].Value;
                                        var def = m.Groups["def"].Value.Trim().TrimEnd(',');
                                        cols.Add((col, def));
                                    }
                                }

                                // check each column existence
                                foreach (var (Name, Definition) in cols)
                                {
                                    await using var colCmd = connection.CreateCommand();
                                    colCmd.CommandText = "SELECT COUNT(*) FROM sys.columns c JOIN sys.tables t ON c.object_id = t.object_id JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = @t AND c.name = @c AND s.name = 'dbo'";
                                    var p1 = colCmd.CreateParameter(); p1.ParameterName = "@t"; p1.Value = tableName; colCmd.Parameters.Add(p1);
                                    var p2 = colCmd.CreateParameter(); p2.ParameterName = "@c"; p2.Value = Name; colCmd.Parameters.Add(p2);
                                    var colExists = (int)(await colCmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("COUNT query did not return a value.")) > 0;
                                    if (!colExists)
                                    {
                                        // Attempt to add column. Use definition as-is for type and nullability.
                                        var alter = $"ALTER TABLE dbo.[{tableName}] ADD [{Name}] {Definition};";
                                        try
                                        {
                                            logger.LogInformation("?? Coluna {Column} ausente na tabela {Table}. Adicionando...", Name, tableName);
                                            await ExecuteSafeAsync(alter, $"ALTER TABLE ADD COLUMN {tableName}.{Name}");
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.LogWarning("?? Falha ao adicionar coluna {Column} na tabela {Table}: {Message} | Exception: {ExceptionDetails}", Name, tableName, LogValueSanitizer.Sanitize(ex.Message), LogValueSanitizer.ExceptionDetails(ex));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // 2) Ensure functions exist
                foreach (var fn in functionBatches)
                {
                    var name = fn.Key;
                    var sql = fn.Value;
                    await using var checkCmd = connection.CreateCommand();
                    checkCmd.CommandText = "SELECT COUNT(*) FROM sys.objects WHERE object_id = OBJECT_ID(@name)";
                    var p = checkCmd.CreateParameter(); p.ParameterName = "@name"; p.Value = name; checkCmd.Parameters.Add(p);
                    var exists = (int)(await checkCmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("COUNT query did not return a value.")) > 0;
                    if (!exists)
                    {
                        logger.LogInformation("?? Função {Name} ausente. Criando...", name);
                        await ExecuteSafeAsync(sql, $"CREATE FUNCTION {name}");
                    }
                }

                // 3) Ensure indexes
                foreach (var (Name, Table, Sql) in indexBatches)
                {
                    if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Table))
                    {
                        await using var checkCmd = connection.CreateCommand();
                        checkCmd.CommandText = "SELECT COUNT(*) FROM sys.indexes i JOIN sys.tables t ON i.object_id = t.object_id JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE i.name = @iname AND t.name = @tname AND s.name = 'dbo'";
                        var p1 = checkCmd.CreateParameter(); p1.ParameterName = "@iname"; p1.Value = Name; checkCmd.Parameters.Add(p1);
                        var p2 = checkCmd.CreateParameter(); p2.ParameterName = "@tname"; p2.Value = Table; checkCmd.Parameters.Add(p2);
                        var exists = (int)(await checkCmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("COUNT query did not return a value.")) > 0;
                        if (!exists)
                        {
                            logger.LogInformation("?? Índice {Index} na tabela {Table} ausente. Criando...", Name, Table);
                            await ExecuteSafeAsync(Sql, $"CREATE INDEX {Name} on {Table}");
                        }
                    }
                    else
                    {
                        // no name/table -> try to execute if seems safe
                        await ExecuteSafeAsync(Sql, "CREATE INDEX (raw)");
                    }
                }

                // 4) Ensure security policy / RLS
                foreach (var sp in securityPolicyBatches)
                {
                    // try detect policy name
                    var m = Regex.Match(sp, @"CREATE\s+SECURITY\s+POLICY\s+\[?(?<name>\w+)\]?", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        var policyName = m.Groups["name"].Value;
                        await using var checkCmd = connection.CreateCommand();
                        checkCmd.CommandText = "SELECT COUNT(*) FROM sys.security_policies WHERE name = @pname";
                        var pp = checkCmd.CreateParameter(); pp.ParameterName = "@pname"; pp.Value = policyName; checkCmd.Parameters.Add(pp);
                        var exists = (int)(await checkCmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("COUNT query did not return a value.")) > 0;
                        if (!exists)
                        {
                            logger.LogInformation("?? Security Policy {Name} ausente. Criando...", policyName);
                            await ExecuteSafeAsync(sp, $"CREATE SECURITY POLICY {policyName}");
                        }
                    }
                    else
                    {
                        // execute generic
                        await ExecuteSafeAsync(sp, "SECURITY POLICY batch");
                    }
                }

                logger.LogInformation("? Validação e aplicação do script de schema concluída");

                await connection.CloseAsync();
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Message.Contains("already an object named") ||
                                                                    sqlEx.Message.Contains("does not exist or you do not have permission"))
        {
            logger.LogError("? Erro de conflito de schema ao aplicar migrations | Exception: {ExceptionDetails}", LogValueSanitizer.ExceptionDetails(sqlEx));
            logger.LogWarning("???????????????????????????????????????????????????????????????");
            logger.LogWarning("?? PROBLEMA DETECTADO: Conflito de schema do banco de dados");
            logger.LogWarning("???????????????????????????????????????????????????????????????");
            logger.LogWarning("");
            logger.LogWarning("?? OPÇÕES DE SOLUÇÃO:");
            logger.LogWarning("");
            logger.LogWarning("1??  OPÇÃO 1 - Reset Completo (Desenvolvimento):");
            logger.LogWarning("   • Deletar o banco de dados completamente");
            logger.LogWarning("   • Reiniciar a aplicação (migrations serão aplicadas do zero)");
            logger.LogWarning("");
            logger.LogWarning("2??  OPÇÃO 2 - Marcar migrations como aplicadas:");
            logger.LogWarning("   • Executar no SQL Server:");
            logger.LogWarning("   INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])");
            logger.LogWarning("   VALUES ('20251230180000_AddApplicationIdToUserSessions', '8.0.0');");
            logger.LogWarning("   • Reiniciar a aplicação");
            logger.LogWarning("");
            logger.LogWarning("3??  OPÇÃO 3 - Desabilitar migrations automáticas:");
            logger.LogWarning("   • No appsettings.json, definir:");
            logger.LogWarning("   \"Database\": {{ \"AutoMigrate\": false }}");
            logger.LogWarning("   • Aplicar migrations manualmente via dotnet ef");
            logger.LogWarning("");
            logger.LogWarning("???????????????????????????????????????????????????????????????");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError("? Erro inesperado ao aplicar migrations | Exception: {ExceptionDetails}", LogValueSanitizer.ExceptionDetails(ex));
            throw;
        }
    }

    /// <summary>
    /// Executa o seeding de dados iniciais
    /// </summary>
    public static async Task SeedDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();

        try
        {
            var context = services.GetRequiredService<GeritDbContext>();
            var seeder = new DatabaseSeeder(context, logger);

            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("? Erro ao executar seeding do banco | Exception: {ExceptionDetails}", LogValueSanitizer.ExceptionDetails(ex));
            throw;
        }
    }

    /// <summary>
    /// Inicializa o banco de dados (migrations + seeding)
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IHost host, bool applyMigrations = false)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<GeritDbContext>>();

        try
        {
            logger.LogInformation("?? Inicializando banco de dados...");

            if (applyMigrations)
            {
                await host.MigrateDatabaseAsync();
            }
            else
            {
                logger.LogInformation("??  Migrations automáticas desabilitadas, pulando...");
            }

            await host.SeedDatabaseAsync();

            logger.LogInformation("? Banco de dados inicializado com sucesso");
        }
        catch (Exception ex)
        {
            logger.LogError("? Erro ao inicializar banco de dados | Exception: {ExceptionDetails}", LogValueSanitizer.ExceptionDetails(ex));
            throw;
        }
    }

    /// <summary>
    /// Verifica a saúde da conexão do banco de dados
    /// </summary>
    public static async Task<bool> CanConnectAsync(this DbContext context, CancellationToken ct = default)
    {
        try
        {
            return await context.Database.CanConnectAsync(ct);
        }
        catch
        {
            return false;
        }
    }
}
