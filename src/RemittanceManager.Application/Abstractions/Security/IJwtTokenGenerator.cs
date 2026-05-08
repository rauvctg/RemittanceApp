using RemittanceManager.Domain.Entities;

namespace RemittanceManager.Application.Abstractions.Security;

/// <summary>
/// Issues signed JWT tokens for authenticated users.
/// </summary>
public interface IJwtTokenGenerator
{
    string Generate(User user);
}
