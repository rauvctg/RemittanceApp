using Microsoft.Data.SqlClient;

namespace RemittanceManager.Infrastructure.Persistence;

public interface ISqlConnectionFactory
{
    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
}

public class SqlConnectionFactory(string connectionString) : ISqlConnectionFactory
{
    private readonly string _connectionString = !string.IsNullOrWhiteSpace(connectionString)
        ? connectionString
        : throw new ArgumentException("Connection string is required.", nameof(connectionString));

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
