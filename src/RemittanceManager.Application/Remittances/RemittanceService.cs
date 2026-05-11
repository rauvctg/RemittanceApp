using RemittanceManager.Application.Abstractions.Time;
using RemittanceManager.Application.Common.Exceptions;
using RemittanceManager.Application.Common.Constants;
using RemittanceManager.Application.Remittances.Dtos;
using RemittanceManager.Domain.Entities;
using RemittanceManager.Domain.Enums;
using RemittanceManager.Domain.Repositories;

namespace RemittanceManager.Application.Remittances;

public class RemittanceService(IRemittanceRepository repo, IClock clock) : IRemittanceService
{
    private readonly IRemittanceRepository _repo = repo;
    private readonly IClock _clock = clock;

    public async Task<RemittanceDto> CreateAsync(Guid userId, CreateRemittanceRequest request, CancellationToken ct)
    {
        var senderIdNumber = (request.SenderIdNumber ?? string.Empty).Trim();
        var beneficiaryIdNumber = (request.BeneficiaryIdNumber ?? string.Empty).Trim();
        IdDocumentValidator.Validate(nameof(request.SenderIdNumber), request.SenderIdType, senderIdNumber);
        IdDocumentValidator.Validate(nameof(request.BeneficiaryIdNumber), request.BeneficiaryIdType, beneficiaryIdNumber);

        var id = Guid.NewGuid();
        var entity = new Remittance
        {
            Id = id,
            UserId = userId,
            SenderName = (request.SenderName ?? string.Empty).Trim(),
            SenderIdType = request.SenderIdType,
            SenderIdNumber = senderIdNumber,
            BeneficiaryName = (request.BeneficiaryName ?? string.Empty).Trim(),
            BeneficiaryIdType = request.BeneficiaryIdType,
            BeneficiaryIdNumber = beneficiaryIdNumber,
            Amount = request.Amount,
            Currency = NormalizeCurrency(request.Currency),
            Status = RemittanceStatus.Pending,
            Date = DateOnly.FromDateTime(_clock.UtcNow),
            CreatedAtUtc = _clock.UtcNow,
            TrackingCode = AppConstants.TrackingCodePrefix + id.ToString("N").Substring(0, 6).ToUpperInvariant()
        };

        await _repo.AddAsync(entity, ct);
        return ToDto(entity);
    }

    public async Task<RemittanceDto> GetByIdAsync(Guid userId, Guid remittanceId, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(remittanceId, userId, ct);
        if (entity is null)
            throw new NotFoundException(string.Format(ErrorMessages.RemittanceNotFound, remittanceId));

        return ToDto(entity);
    }

    public async Task<IEnumerable<RemittanceDto>> ListAsync(Guid userId, CancellationToken ct)
    {
        var items = await _repo.ListByUserAsync(userId, ct);
        return items.Select(ToDto);
    }

    public async Task<RemittanceDto> UpdateAsync(Guid userId, Guid remittanceId, UpdateRemittanceRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(remittanceId, userId, ct);
        if (entity is null)
            throw new NotFoundException(string.Format(ErrorMessages.RemittanceNotFound, remittanceId));

        EnsureValidStatusTransition(entity.Status, request.Status);

        if (entity.Status != RemittanceStatus.Pending)
        {
            EnsureBusinessFieldsUnchanged(entity, request);
            entity.Status = request.Status;
        }
        else
        {
            var senderIdNumber = (request.SenderIdNumber ?? string.Empty).Trim();
            var beneficiaryIdNumber = (request.BeneficiaryIdNumber ?? string.Empty).Trim();
            IdDocumentValidator.Validate(nameof(request.SenderIdNumber), request.SenderIdType, senderIdNumber);
            IdDocumentValidator.Validate(nameof(request.BeneficiaryIdNumber), request.BeneficiaryIdType, beneficiaryIdNumber);

            entity.SenderName = (request.SenderName ?? string.Empty).Trim();
            entity.SenderIdType = request.SenderIdType;
            entity.SenderIdNumber = senderIdNumber;
            entity.BeneficiaryName = (request.BeneficiaryName ?? string.Empty).Trim();
            entity.BeneficiaryIdType = request.BeneficiaryIdType;
            entity.BeneficiaryIdNumber = beneficiaryIdNumber;
            entity.Amount = request.Amount;
            entity.Currency = NormalizeCurrency(request.Currency);
            entity.Status = request.Status;
        }

        entity.UpdatedAtUtc = _clock.UtcNow;

        await _repo.UpdateAsync(entity, ct);
        return ToDto(entity);
    }

    public async Task DeleteAsync(Guid userId, Guid remittanceId, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(remittanceId, userId, ct);
        if (entity is null)
            throw new NotFoundException(string.Format(ErrorMessages.RemittanceNotFound, remittanceId));

        if (entity.Status != RemittanceStatus.Pending && entity.Status != RemittanceStatus.Cancelled)
            throw new ConflictException(string.Format(ErrorMessages.CannotDeleteRemittance, entity.Status));

        var deleted = await _repo.DeleteAsync(remittanceId, userId, ct);
        if (!deleted)
            throw new NotFoundException(string.Format(ErrorMessages.RemittanceNotFound, remittanceId));
    }

    public async Task<TrackingResult?> GetByTrackingCodeAsync(string trackingCode, CancellationToken ct)
    {
        var code = (trackingCode ?? string.Empty).Trim().ToUpperInvariant();
        var entity = await _repo.GetByTrackingCodeAsync(code, ct);
        if (entity is null)
            return null;

        return new TrackingResult
        {
            TrackingCode = entity.TrackingCode,
            SenderName = entity.SenderName,
            SenderIdType = entity.SenderIdType,
            SenderIdNumber = entity.SenderIdNumber,
            BeneficiaryName = entity.BeneficiaryName,
            BeneficiaryIdType = entity.BeneficiaryIdType,
            BeneficiaryIdNumber = entity.BeneficiaryIdNumber,
            Amount = entity.Amount,
            Currency = entity.Currency,
            Status = entity.Status,
            Date = entity.Date
        };
    }

    private static string NormalizeCurrency(string currency)
    {
        return (currency ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static void EnsureValidStatusTransition(RemittanceStatus current, RemittanceStatus next)
    {
        if (current == next)
            return;

        var allowed = current switch
        {
            RemittanceStatus.Pending => next == RemittanceStatus.Sent || next == RemittanceStatus.Cancelled,
            RemittanceStatus.Sent => next == RemittanceStatus.Completed || next == RemittanceStatus.Cancelled,
            RemittanceStatus.Completed => false,
            RemittanceStatus.Cancelled => false,
            _ => false
        };

        if (!allowed)
            throw new ConflictException(string.Format(ErrorMessages.InvalidStatusTransition, current, next));
    }

    private static void EnsureBusinessFieldsUnchanged(Remittance entity, UpdateRemittanceRequest request)
    {
        var changes = new List<string>();

        if (!string.Equals(entity.SenderName, (request.SenderName ?? string.Empty).Trim(), StringComparison.Ordinal))
            changes.Add(nameof(request.SenderName));
        if (entity.SenderIdType != request.SenderIdType)
            changes.Add(nameof(request.SenderIdType));
        if (!string.Equals(entity.SenderIdNumber, (request.SenderIdNumber ?? string.Empty).Trim(), StringComparison.Ordinal))
            changes.Add(nameof(request.SenderIdNumber));
        if (!string.Equals(entity.BeneficiaryName, (request.BeneficiaryName ?? string.Empty).Trim(), StringComparison.Ordinal))
            changes.Add(nameof(request.BeneficiaryName));
        if (entity.BeneficiaryIdType != request.BeneficiaryIdType)
            changes.Add(nameof(request.BeneficiaryIdType));
        if (!string.Equals(entity.BeneficiaryIdNumber, (request.BeneficiaryIdNumber ?? string.Empty).Trim(), StringComparison.Ordinal))
            changes.Add(nameof(request.BeneficiaryIdNumber));
        if (entity.Amount != request.Amount)
            changes.Add(nameof(request.Amount));
        if (!string.Equals(entity.Currency, NormalizeCurrency(request.Currency), StringComparison.Ordinal))
            changes.Add(nameof(request.Currency));

        if (changes.Count > 0)
            throw new ConflictException(string.Format(ErrorMessages.CannotModifyFields, string.Join(", ", changes), entity.Status));
    }

    private static RemittanceDto ToDto(Remittance r)
    {
        return new RemittanceDto
        {
            Id = r.Id,
            UserId = r.UserId,
            SenderName = r.SenderName,
            SenderIdType = r.SenderIdType,
            SenderIdNumber = r.SenderIdNumber,
            BeneficiaryName = r.BeneficiaryName,
            BeneficiaryIdType = r.BeneficiaryIdType,
            BeneficiaryIdNumber = r.BeneficiaryIdNumber,
            Amount = r.Amount,
            Currency = r.Currency,
            Status = r.Status,
            Date = r.Date,
            CreatedAtUtc = r.CreatedAtUtc,
            UpdatedAtUtc = r.UpdatedAtUtc,
            TrackingCode = r.TrackingCode
        };
    }
}
