namespace RemittanceManager.Domain.Enums;

/// <summary>
/// Lifecycle status of a remittance from the user's perspective.
/// </summary>
public enum RemittanceStatus
{
    Pending = 0,
    Sent = 1,
    Completed = 2,
    Cancelled = 3
}
