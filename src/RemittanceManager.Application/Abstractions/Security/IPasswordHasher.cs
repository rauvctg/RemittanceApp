namespace RemittanceManager.Application.Abstractions.Security;

/// <summary>
/// Hashes and verifies passwords. Implementation lives in Infrastructure (e.g. BCrypt).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
