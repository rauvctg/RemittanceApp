using System.Data;
using Microsoft.Data.SqlClient;
using RemittanceManager.Domain.Entities;
using RemittanceManager.Domain.Repositories;

namespace RemittanceManager.Infrastructure.Persistence.Repositories;

public class UserRepository(ISqlConnectionFactory factory) : IUserRepository
{
    private readonly ISqlConnectionFactory _factory = factory;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        const string sql = @"
            SELECT Id, Email, PasswordHash, DisplayName, CreatedAtUtc
            FROM dbo.Users
            WHERE Id = @Id;";

        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        const string sql = @"
                SELECT Id, Email, PasswordHash, DisplayName, CreatedAtUtc
                FROM dbo.Users
                WHERE Email = @Email;";

        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = email;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task AddAsync(User user, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO dbo.Users (Id, Email, PasswordHash, DisplayName, CreatedAtUtc)
            VALUES (@Id, @Email, @PasswordHash, @DisplayName, @CreatedAtUtc);";

        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = user.Id;
        cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = user.Email;
        cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 200).Value = user.PasswordHash;
        cmd.Parameters.Add("@DisplayName", SqlDbType.NVarChar, 100).Value = user.DisplayName;
        cmd.Parameters.Add("@CreatedAtUtc", SqlDbType.DateTime2).Value = user.CreatedAtUtc;

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static User Map(SqlDataReader r)
    {
        return new User
        {
            Id = r.GetGuid(0),
            Email = r.GetString(1),
            PasswordHash = r.GetString(2),
            DisplayName = r.GetString(3),
            CreatedAtUtc = DateTime.SpecifyKind(r.GetDateTime(4), DateTimeKind.Utc)
        };
    }
}
