namespace RemittanceManager.Application.Abstractions.Time;

/// <summary>
/// Abstraction over DateTime.UtcNow to keep services deterministic and testable.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
