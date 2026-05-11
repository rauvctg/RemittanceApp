namespace RemittanceManager.Infrastructure.Security;

/// <summary>
/// Strongly-typed JWT settings bound from configuration (section AppConstants.JwtSection).
/// </summary>

using RemittanceManager.Application.Common.Constants;

public class JwtSettings
{
    public const string SectionName = AppConstants.JwtSection;

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
