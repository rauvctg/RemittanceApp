using RemittanceManager.Application.Common.Constants;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RemittanceManager.Application.Abstractions.Security;
using RemittanceManager.Application.Abstractions.Time;
using RemittanceManager.Domain.Entities;

namespace RemittanceManager.Infrastructure.Security;

public class JwtTokenGenerator(IOptions<JwtSettings> settings, IClock clock) : IJwtTokenGenerator
{
    private readonly JwtSettings _settings = settings.Value;
    private readonly IClock _clock = clock;

    public string Generate(User user)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (string.IsNullOrWhiteSpace(_settings.SigningKey))
            throw new InvalidOperationException("JWT signing key is not configured.");

        var keyBytes = Encoding.UTF8.GetBytes(_settings.SigningKey);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypesCustom.DisplayName, user.DisplayName)
        };

        var now = _clock.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_settings.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
