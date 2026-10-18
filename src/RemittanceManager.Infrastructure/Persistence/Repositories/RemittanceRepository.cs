using System.Data;
using Microsoft.Data.SqlClient;
using RemittanceManager.Domain.Entities;
using RemittanceManager.Domain.Enums;
using RemittanceManager.Domain.Repositories;

namespace RemittanceManager.Infrastructure.Persistence.Repositories;

public class RemittanceRepository(ISqlConnectionFactory factory) : IRemittanceRepository
{
    private readonly ISqlConnectionFactory _factory = factory;

    private const string SelectColumns = @"
        SELECT Id, UserId, BeneficiaryName, Amount, Currency, Status, [Date], CreatedAtUtc, TrackingCode,
               SenderName, SenderIdType, SenderIdNumber, BeneficiaryIdType, BeneficiaryIdNumber, UpdatedAtUtc
        FROM dbo.Remittances";

    public async Task<Remittance?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var sql = SelectColumns + " WHERE Id = @Id AND UserId = @UserId AND IsDeleted = 0;";

        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
        cmd.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = userId;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<Remittance>> ListByUserAsync(Guid userId, CancellationToken ct)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand("dbo.sp_Remittances_ListByUser", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = userId;

        var list = new List<Remittance>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(Map(reader));
        }

        return list;
    }

    public async Task AddAsync(Remittance remittance, CancellationToken ct)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand("dbo.sp_Remittances_Insert", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        BindParameters(cmd, remittance);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> UpdateAsync(Remittance remittance, CancellationToken ct)
    {
        const string sql = @"
            UPDATE dbo.Remittances
            SET BeneficiaryName = @BeneficiaryName,
                Amount = @Amount,
                Currency = @Currency,
                Status = @Status,
                [Date] = @Date,
                SenderName = @SenderName,
                SenderIdType = @SenderIdType,
                SenderIdNumber = @SenderIdNumber,
                BeneficiaryIdType = @BeneficiaryIdType,
                BeneficiaryIdNumber = @BeneficiaryIdNumber,
                UpdatedAtUtc = @UpdatedAtUtc
            WHERE Id = @Id AND UserId = @UserId AND IsDeleted = 0;";

        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        BindParameters(cmd, remittance, includeCreatedAt: false);
        cmd.Parameters.Add("@UpdatedAtUtc", SqlDbType.DateTime2).Value = (object?)remittance.UpdatedAtUtc ?? DBNull.Value;
        var rows = await cmd.ExecuteNonQueryAsync(ct);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand("dbo.sp_Remittances_SoftDelete", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
        cmd.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = userId;
        var result = await cmd.ExecuteScalarAsync(ct);
        var rows = result is int i ? i : Convert.ToInt32(result);
        return rows > 0;
    }

    public async Task<Remittance?> GetByTrackingCodeAsync(string trackingCode, CancellationToken ct)
    {
        var sql = SelectColumns + " WHERE TrackingCode = @TrackingCode AND IsDeleted = 0;";

        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@TrackingCode", SqlDbType.NVarChar, 20).Value = trackingCode;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    private static void BindParameters(SqlCommand cmd, Remittance r, bool includeCreatedAt = true)
    {
        cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = r.Id;
        cmd.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = r.UserId;
        cmd.Parameters.Add("@BeneficiaryName", SqlDbType.NVarChar, 200).Value = r.BeneficiaryName;
        cmd.Parameters.Add("@Amount", SqlDbType.Decimal).Value = r.Amount;
        cmd.Parameters["@Amount"].Precision = 18;
        cmd.Parameters["@Amount"].Scale = 2;
        cmd.Parameters.Add("@Currency", SqlDbType.NVarChar, 3).Value = r.Currency;
        cmd.Parameters.Add("@Status", SqlDbType.Int).Value = (int)r.Status;
        cmd.Parameters.Add("@Date", SqlDbType.Date).Value = r.Date.ToDateTime(TimeOnly.MinValue);
        if (includeCreatedAt)
            cmd.Parameters.Add("@CreatedAtUtc", SqlDbType.DateTime2).Value = r.CreatedAtUtc;
        cmd.Parameters.Add("@TrackingCode", SqlDbType.NVarChar, 20).Value = r.TrackingCode;
        cmd.Parameters.Add("@SenderName", SqlDbType.NVarChar, 200).Value = r.SenderName;
        cmd.Parameters.Add("@SenderIdType", SqlDbType.Int).Value = (int)r.SenderIdType;
        cmd.Parameters.Add("@SenderIdNumber", SqlDbType.NVarChar, 50).Value = r.SenderIdNumber;
        cmd.Parameters.Add("@BeneficiaryIdType", SqlDbType.Int).Value = (int)r.BeneficiaryIdType;
        cmd.Parameters.Add("@BeneficiaryIdNumber", SqlDbType.NVarChar, 50).Value = r.BeneficiaryIdNumber;
    }

    private static Remittance Map(SqlDataReader r)
    {
        return new Remittance
        {
            Id = r.GetGuid(0),
            UserId = r.GetGuid(1),
            BeneficiaryName = r.GetString(2),
            Amount = r.GetDecimal(3),
            Currency = r.GetString(4),
            Status = (RemittanceStatus)r.GetInt32(5),
            Date = DateOnly.FromDateTime(r.GetDateTime(6)),
            CreatedAtUtc = DateTime.SpecifyKind(r.GetDateTime(7), DateTimeKind.Utc),
            TrackingCode = r.GetString(8),
            SenderName = r.GetString(9),
            SenderIdType = (IdDocumentType)r.GetInt32(10),
            SenderIdNumber = r.GetString(11),
            BeneficiaryIdType = (IdDocumentType)r.GetInt32(12),
            BeneficiaryIdNumber = r.GetString(13),
            UpdatedAtUtc = r.IsDBNull(14) ? null : DateTime.SpecifyKind(r.GetDateTime(14), DateTimeKind.Utc)
        };
    }
}
