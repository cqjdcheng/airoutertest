using System.Text;
using CheapAI.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using MySqlConnector;
using SqlSugar;

namespace CheapAI.Api.HostedServices;

public sealed class DatabaseInitializationHostedService(
    ISqlSugarClient db,
    IHostEnvironment environment,
    ILogger<DatabaseInitializationHostedService> logger,
    IOptions<MySqlOptions> mySqlOptions) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await EnsureDatabaseExistsAsync(cancellationToken);

            var scriptsPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "CheapAI.Infrastructure", "Sql", "Init"));
            if (!Directory.Exists(scriptsPath))
            {
                logger.LogWarning("Database init scripts path not found: {ScriptsPath}", scriptsPath);
                return;
            }

            var files = Directory.GetFiles(scriptsPath, "*.sql").OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
            if (files.Count == 0)
            {
                logger.LogInformation("No database init scripts found.");
                return;
            }

            logger.LogInformation("Initializing database schema using {Count} scripts.", files.Count);

            foreach (var file in files)
            {
                var content = await File.ReadAllTextAsync(file, Encoding.UTF8, cancellationToken);
                var statements = content
                    .Split("--//@", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x));

                foreach (var statement in statements)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await db.Ado.ExecuteCommandAsync(statement);
                }
            }

            await EnsureSelfTestProbeColumnsAsync(cancellationToken);

            logger.LogInformation("Database schema initialization complete for {ConnectionString}", mySqlOptions.Value.ConnectionString);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Database initialization skipped because MySQL is not currently reachable.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        var builder = new MySqlConnectionStringBuilder(mySqlOptions.Value.ConnectionString);
        var databaseName = builder.Database;

        builder.Database = string.Empty;

        await using var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureSelfTestProbeColumnsAsync(CancellationToken cancellationToken)
    {
        var columns = new (string Name, string Definition)[]
        {
            ("match_score", "ALTER TABLE self_tests ADD COLUMN match_score DECIMAL(10, 4) NOT NULL DEFAULT 0 AFTER result_summary;"),
            ("input_tokens", "ALTER TABLE self_tests ADD COLUMN input_tokens INT NULL AFTER match_score;"),
            ("output_tokens", "ALTER TABLE self_tests ADD COLUMN output_tokens INT NULL AFTER input_tokens;"),
            ("total_tokens", "ALTER TABLE self_tests ADD COLUMN total_tokens INT NULL AFTER output_tokens;"),
            ("estimated_tokens", "ALTER TABLE self_tests ADD COLUMN estimated_tokens INT NOT NULL DEFAULT 1000 AFTER total_tokens;"),
            ("tokens_per_second", "ALTER TABLE self_tests ADD COLUMN tokens_per_second DECIMAL(10, 2) NULL AFTER estimated_tokens;"),
            ("checks_json", "ALTER TABLE self_tests ADD COLUMN checks_json JSON NULL AFTER tokens_per_second;")
        };

        await using var connection = new MySqlConnection(mySqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var column in columns)
        {
            if (await ColumnExistsAsync(connection, column.Name, cancellationToken))
            {
                continue;
            }

            await using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = column.Definition;
            await alterCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<bool> ColumnExistsAsync(MySqlConnection connection, string columnName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'self_tests'
              AND COLUMN_NAME = @columnName;
            """;
        command.Parameters.AddWithValue("@columnName", columnName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }
}
