using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace JayTom.Dws.Infrastructure.Migrations;

/// <summary>
/// Separates current-model bootstrapping from versioned upgrades of existing SQLite databases.
/// </summary>
internal static class SqliteSchemaMigrator
{
    /// <summary>
    /// Creates an empty database from the current model, or upgrades an existing database without
    /// assuming that every optional module table is present.
    /// </summary>
    public static void Apply(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        IRelationalDatabaseCreator databaseCreator =
            context.GetService<IRelationalDatabaseCreator>();
        if (!databaseCreator.Exists())
        {
            databaseCreator.Create();
        }

        if (!databaseCreator.HasTables())
        {
            databaseCreator.CreateTables();
            StampCurrentModelBaseline(context);
            return;
        }

        ApplyAdaptiveCompatibilityChanges(context.Database.GetDbConnection());
        if (context.Database.GetMigrations().Any())
        {
            context.Database.Migrate();
        }
    }

    /// <summary>
    /// Marks migrations as applied after the current EF model has created a new database. Running
    /// their historical upgrade SQL against that current schema would duplicate columns.
    /// </summary>
    private static void StampCurrentModelBaseline(DbContext context)
    {
        string[] migrations = context.Database.GetMigrations().ToArray();
        if (migrations.Length == 0)
        {
            return;
        }

        IHistoryRepository history = context.GetService<IHistoryRepository>();
        DbConnection connection = context.Database.GetDbConnection();
        bool shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            Execute(connection, history.GetCreateIfNotExistsScript());
            foreach (string migration in migrations)
            {
                Execute(
                    connection,
                    history.GetInsertScript(
                        new HistoryRow(migration, ProductInfo.GetVersion())));
            }
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }

    /// <summary>Adds new compatibility columns only when their owning legacy table exists.</summary>
    private static void ApplyAdaptiveCompatibilityChanges(DbConnection connection)
    {
        bool shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            EnsureColumn(
                connection,
                "Data_SoundInfo",
                "SoundFileReference",
                "TEXT NOT NULL DEFAULT ''");
            EnsureColumn(
                connection,
                "Conf_LogisticsCodeRecognitionInfo",
                "SoundFileReference",
                "TEXT NULL");
            EnsureColumn(
                connection,
                "Conf_LogisticsCodeRecognitionInfo",
                "IconFileReference",
                "TEXT NULL");
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }

    /// <summary>Adds a column exactly once while tolerating databases with optional modules.</summary>
    private static void EnsureColumn(
        DbConnection connection,
        string tableName,
        string columnName,
        string columnDefinition)
    {
        if (!TableExists(connection, tableName) || ColumnExists(connection, tableName, columnName))
        {
            return;
        }

        Execute(
            connection,
            $"ALTER TABLE {QuoteIdentifier(tableName)} ADD COLUMN {QuoteIdentifier(columnName)} {columnDefinition};");
    }

    /// <summary>Returns whether an SQLite table is present.</summary>
    private static bool TableExists(DbConnection connection, string tableName)
    {
        using DbCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @tableName;";
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);
        return Convert.ToInt64(command.ExecuteScalar()) > 0;
    }

    /// <summary>Returns whether an SQLite table already contains a column.</summary>
    private static bool ColumnExists(
        DbConnection connection,
        string tableName,
        string columnName)
    {
        using DbCommand command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({QuoteIdentifier(tableName)});";
        using DbDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Executes trusted schema SQL produced locally or by EF Core.</summary>
    private static void Execute(DbConnection connection, string commandText)
    {
        using DbCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }

    /// <summary>Quotes a compile-time-controlled SQLite identifier.</summary>
    private static string QuoteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
