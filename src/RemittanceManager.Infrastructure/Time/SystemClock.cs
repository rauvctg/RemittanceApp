using RemittanceManager.Application.Abstractions.Time;

namespace RemittanceManager.Infrastructure.Time;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
