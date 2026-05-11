using RemittanceManager.Domain.Enums;

namespace RemittanceManager.Domain.Entities;

public class Remittance
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public IdDocumentType SenderIdType { get; set; }
    public string SenderIdNumber { get; set; } = string.Empty;
    public string BeneficiaryName { get; set; } = string.Empty;
    public IdDocumentType BeneficiaryIdType { get; set; }
    public string BeneficiaryIdNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public RemittanceStatus Status { get; set; }
    public DateOnly Date { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string TrackingCode { get; set; } = string.Empty;
}
