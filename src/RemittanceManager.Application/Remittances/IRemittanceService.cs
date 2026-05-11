using RemittanceManager.Application.Remittances.Dtos;

namespace RemittanceManager.Application.Remittances;

public interface IRemittanceService
{
    Task<RemittanceDto> CreateAsync(Guid userId, CreateRemittanceRequest request, CancellationToken ct);
    Task<RemittanceDto> GetByIdAsync(Guid userId, Guid remittanceId, CancellationToken ct);
    Task<IEnumerable<RemittanceDto>> ListAsync(Guid userId, CancellationToken ct);
    Task<RemittanceDto> UpdateAsync(Guid userId, Guid remittanceId, UpdateRemittanceRequest request, CancellationToken ct);
    Task DeleteAsync(Guid userId, Guid remittanceId, CancellationToken ct);
    Task<TrackingResult?> GetByTrackingCodeAsync(string trackingCode, CancellationToken ct);
}
