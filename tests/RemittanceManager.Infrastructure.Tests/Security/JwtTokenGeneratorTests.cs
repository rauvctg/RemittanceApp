using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using RemittanceManager.Application.Abstractions.Time;
using RemittanceManager.Domain.Entities;
using RemittanceManager.Infrastructure.Security;

namespace RemittanceManager.Infrastructure.Tests.Security;

public class JwtTokenGeneratorTests
{
    private readonly JwtSettings _settings = new()
    {
        Issuer = "RemittanceManager",
        Audience = "RemittanceManager.Client",
        SigningKey = "test-signing-key-please-make-this-long-enough-32+",
        ExpirationMinutes = 60
    };

    private readonly Mock<IClock> _clock = new();
    private readonly DateTime _now = new(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc);

    public JwtTokenGeneratorTests()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(_now);
    }

    private JwtTokenGenerator CreateSut()
    {
        return new JwtTokenGenerator(Options.Create(_settings), _clock.Object);
    }

    private static User SampleUser()
    {
        return new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "maria@test.com",
            DisplayName = "Maria",
            PasswordHash = "irrelevant"
        };
    }

    [Fact]
    public void Generate_ReturnsTokenWithExpectedClaimsAndIssuerAudience()
    {
        var sut = CreateSut();
        var user = SampleUser();

        var rawToken = sut.Generate(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(rawToken);

        jwt.Issuer.Should().Be(_settings.Issuer);
        jwt.Audiences.Should().Contain(_settings.Audience);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwt.Claims.Should().Contain(c => c.Type == "displayName" && c.Value == user.DisplayName);
    }

    [Fact]
    public void Generate_TokenExpiresAfterConfiguredMinutes()
    {
        var sut = CreateSut();

        var rawToken = sut.Generate(SampleUser());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(rawToken);

        jwt.ValidTo.Should().BeCloseTo(_now.AddMinutes(_settings.ExpirationMinutes), TimeSpan.FromSeconds(1));
        jwt.ValidFrom.Should().BeCloseTo(_now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Generate_TokenIsSignedWithConfiguredKey()
    {
        var sut = CreateSut();
        var rawToken = sut.Generate(SampleUser());

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey)),
            ValidateLifetime = false,
            ClockSkew = TimeSpan.Zero
        };

        var handler = new JwtSecurityTokenHandler();
        var act = () => handler.ValidateToken(rawToken, validationParameters, out _);

        act.Should().NotThrow();
    }

    [Fact]
    public void Generate_WithEmptySigningKey_Throws()
    {
        _settings.SigningKey = "";
        var sut = CreateSut();

        var act = () => sut.Generate(SampleUser());

        act.Should().Throw<InvalidOperationException>();
    }
}
