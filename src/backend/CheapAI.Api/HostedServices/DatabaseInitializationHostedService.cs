using System.Text;
using CheapAI.Application.RelaySites;
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

            await EnsureExistingSchemaCompatibilityAsync(cancellationToken);

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

            await EnsureExistingSchemaCompatibilityAsync(cancellationToken);

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

    private async Task EnsureExistingSchemaCompatibilityAsync(CancellationToken cancellationToken)
    {
        await EnsureModelProviderSchemaAsync(cancellationToken);
        await EnsureSelfTestProbeColumnsAsync(cancellationToken);
        await EnsureUnifiedTestRecordColumnsAsync(cancellationToken);
        await EnsureModelDisplayColumnsAsync(cancellationToken);
    }

    private async Task EnsureModelProviderSchemaAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(mySqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, "model_providers", cancellationToken))
        {
            await using var createCommand = connection.CreateCommand();
            createCommand.CommandText = """
                CREATE TABLE IF NOT EXISTS model_providers (
                  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                  slug VARCHAR(64) NOT NULL,
                  name VARCHAR(128) NOT NULL,
                  website_url VARCHAR(255) NULL,
                  description TEXT NULL,
                  status VARCHAR(24) NOT NULL DEFAULT 'active',
                  sort_order INT NOT NULL DEFAULT 1000,
                  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
                  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
                  deleted_at DATETIME(3) NULL,
                  PRIMARY KEY (id),
                  UNIQUE KEY uk_model_providers_slug (slug),
                  KEY idx_model_providers_status_sort (status, sort_order)
                );
                """;
            await createCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        if (!await TableExistsAsync(connection, "models", cancellationToken))
        {
            return;
        }

        if (!await ColumnExistsAsync(connection, "models", "provider_id", cancellationToken))
        {
            await using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = "ALTER TABLE models ADD COLUMN provider_id BIGINT UNSIGNED NULL AFTER id;";
            await alterCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await EnsureIndexAsync(connection, "models", "idx_models_provider_id", "ALTER TABLE models ADD INDEX idx_models_provider_id (provider_id);", cancellationToken);

        var vendors = new List<string>();
        await using (var vendorCommand = connection.CreateCommand())
        {
            vendorCommand.CommandText = "SELECT DISTINCT vendor FROM models WHERE vendor IS NOT NULL AND vendor <> '';";
            await using var reader = await vendorCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                vendors.Add(reader.GetString(0));
            }
        }

        foreach (var vendor in vendors)
        {
            var slug = SlugHelper.Normalize(null, vendor);
            await using var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = """
                INSERT INTO model_providers (slug, name, status, sort_order, created_at, updated_at)
                SELECT @slug, @name, 'active', 1000, CURRENT_TIMESTAMP(3), CURRENT_TIMESTAMP(3)
                WHERE NOT EXISTS (SELECT 1 FROM model_providers WHERE slug = @slug);
                """;
            insertCommand.Parameters.AddWithValue("@slug", slug);
            insertCommand.Parameters.AddWithValue("@name", vendor);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var backfillCommand = connection.CreateCommand();
        backfillCommand.CommandText = """
            UPDATE models m
            INNER JOIN model_providers p ON p.name = m.vendor
            SET m.provider_id = p.id
            WHERE m.provider_id IS NULL;
            """;
        await backfillCommand.ExecuteNonQueryAsync(cancellationToken);
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

        if (!await TableExistsAsync(connection, "self_tests", cancellationToken))
        {
            return;
        }

        foreach (var column in columns)
        {
            if (await ColumnExistsAsync(connection, "self_tests", column.Name, cancellationToken))
            {
                continue;
            }

            await using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = column.Definition;
            await alterCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task EnsureUnifiedTestRecordColumnsAsync(CancellationToken cancellationToken)
    {
        var columns = new (string Name, string Definition)[]
        {
            ("site_url", "ALTER TABLE test_records ADD COLUMN site_url VARCHAR(255) NULL AFTER channel_id;"),
            ("site_name", "ALTER TABLE test_records ADD COLUMN site_name VARCHAR(128) NULL AFTER site_url;"),
            ("model_slug", "ALTER TABLE test_records ADD COLUMN model_slug VARCHAR(128) NULL AFTER site_name;"),
            ("model_name", "ALTER TABLE test_records ADD COLUMN model_name VARCHAR(128) NULL AFTER model_slug;"),
            ("is_stream", "ALTER TABLE test_records ADD COLUMN is_stream TINYINT(1) NOT NULL DEFAULT 1 AFTER test_type;"),
            ("risk_score", "ALTER TABLE test_records ADD COLUMN risk_score DECIMAL(10, 4) NOT NULL DEFAULT 0 AFTER detected_model_id;"),
            ("risk_level", "ALTER TABLE test_records ADD COLUMN risk_level VARCHAR(24) NOT NULL DEFAULT 'low' AFTER risk_score;"),
            ("result_summary", "ALTER TABLE test_records ADD COLUMN result_summary VARCHAR(512) NULL AFTER risk_level;"),
            ("match_score", "ALTER TABLE test_records ADD COLUMN match_score DECIMAL(10, 4) NOT NULL DEFAULT 0 AFTER result_summary;"),
            ("input_tokens", "ALTER TABLE test_records ADD COLUMN input_tokens INT NULL AFTER match_score;"),
            ("output_tokens", "ALTER TABLE test_records ADD COLUMN output_tokens INT NULL AFTER input_tokens;"),
            ("total_tokens", "ALTER TABLE test_records ADD COLUMN total_tokens INT NULL AFTER output_tokens;"),
            ("estimated_tokens", "ALTER TABLE test_records ADD COLUMN estimated_tokens INT NOT NULL DEFAULT 1000 AFTER total_tokens;"),
            ("tokens_per_second", "ALTER TABLE test_records ADD COLUMN tokens_per_second DECIMAL(10, 2) NULL AFTER estimated_tokens;"),
            ("checks_json", "ALTER TABLE test_records ADD COLUMN checks_json JSON NULL AFTER tokens_per_second;"),
            ("self_test_id", "ALTER TABLE test_records ADD COLUMN self_test_id CHAR(36) NULL AFTER checks_json;")
        };

        await using var connection = new MySqlConnection(mySqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, "test_records", cancellationToken))
        {
            return;
        }

        foreach (var column in columns)
        {
            if (await ColumnExistsAsync(connection, "test_records", column.Name, cancellationToken))
            {
                continue;
            }

            await using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = column.Definition;
            await alterCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await EnsureIndexAsync(connection, "test_records", "idx_test_records_self_test_id", "ALTER TABLE test_records ADD INDEX idx_test_records_self_test_id (self_test_id);", cancellationToken);
    }

    private async Task EnsureModelDisplayColumnsAsync(CancellationToken cancellationToken)
    {
        var columns = new (string Name, string Definition)[]
        {
            ("is_hot", "ALTER TABLE models ADD COLUMN is_hot TINYINT(1) NOT NULL DEFAULT 0 AFTER status;"),
            ("sort_order", "ALTER TABLE models ADD COLUMN sort_order INT NOT NULL DEFAULT 1000 AFTER is_hot;"),
            ("capability_score", "ALTER TABLE models ADD COLUMN capability_score DECIMAL(10, 4) NULL AFTER official_output_price_usd;"),
            ("capability_source", "ALTER TABLE models ADD COLUMN capability_source VARCHAR(64) NULL AFTER capability_score;"),
            ("capability_updated_at", "ALTER TABLE models ADD COLUMN capability_updated_at DATETIME(3) NULL AFTER capability_source;")
        };

        await using var connection = new MySqlConnection(mySqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, "models", cancellationToken))
        {
            return;
        }

        foreach (var column in columns)
        {
            if (await ColumnExistsAsync(connection, "models", column.Name, cancellationToken))
            {
                continue;
            }

            await using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = column.Definition;
            await alterCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<bool> TableExistsAsync(MySqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName;
            """;
        command.Parameters.AddWithValue("@tableName", tableName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task<bool> ColumnExistsAsync(MySqlConnection connection, string tableName, string columnName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName
              AND COLUMN_NAME = @columnName;
            """;
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task EnsureIndexAsync(MySqlConnection connection, string tableName, string indexName, string statement, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName
              AND INDEX_NAME = @indexName;
            """;
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@indexName", indexName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (Convert.ToInt32(result) > 0)
        {
            return;
        }

        await using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = statement;
        await alterCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}
