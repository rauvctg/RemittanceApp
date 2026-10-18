using RemittanceManager.Domain.Entities;

namespace RemittanceManager.Domain.Repositories;

public interface IRemittanceRepository
{
    Task<Remittance?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct);
    Task<Remittance?> GetByTrackingCodeAsync(string trackingCode, CancellationToken ct);
    Task<IReadOnlyList<Remittance>> ListByUserAsync(Guid userId, CancellationToken ct);
    Task AddAsync(Remittance remittance, CancellationToken ct);
    Task<bool> UpdateAsync(Remittance remittance, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct);
}
