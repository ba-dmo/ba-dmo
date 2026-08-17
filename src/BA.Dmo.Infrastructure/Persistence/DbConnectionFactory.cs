using System.Data;
using BA.Dmo.Application.Shared.Persistence;
using Npgsql;

namespace BA.Dmo.Infrastructure.Persistence;

/// <summary>
/// Npgsql connection factory of the persistence foundation (Plan-V3 U-03,
/// GLM-DATA-01: Npgsql + Dapper against Supabase PostgreSQL, schema public).
/// Every <see cref="OpenConnectionAsync"/> call creates and opens an
/// independent connection; callers own its disposal (typically through
/// DapperUnitOfWork). No global/static connection exists anywhere.
/// </summary>
public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new DatabaseConnectionException(
                $"Missing database connection. Set the environment variable " +
                $"'{DatabaseConnectionSettings.ConnectionStringVariable}' (or " +
                $"'{DatabaseConnectionSettings.FallbackConnectionStringVariable}').");

        _connectionString = connectionString;
    }

    /// <summary>
    /// Builds a factory from the approved environment contract. Fails clearly
    /// when the configuration is absent.
    /// </summary>
    public static DbConnectionFactory FromEnvironment(Func<string, string?> environment)
    {
        var connectionString = DatabaseConnectionSettings.ResolveConnectionString(environment);
        return connectionString is null
            ? throw new DatabaseConnectionException(
                $"Missing database connection. Set the environment variable " +
                $"'{DatabaseConnectionSettings.ConnectionStringVariable}' (or " +
                $"'{DatabaseConnectionSettings.FallbackConnectionStringVariable}'). " +
                "No connection string is ever stored in the repository.")
            : new DbConnectionFactory(connectionString);
    }

    /// <summary>Exposed for diagnostics/tests: never logged elsewhere.</summary>
    public string ConnectionString => _connectionString;

    public async Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await connection.DisposeAsync();
            // Translated, diagnostic, and safe: no credentials in the message.
            throw new DatabaseConnectionException(
                $"Unable to open the database connection ({ex.GetType().Name}: {ex.Message}).",
                ex);
        }
    }
}
