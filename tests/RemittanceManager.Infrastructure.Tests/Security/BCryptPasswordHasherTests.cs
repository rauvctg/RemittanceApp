using FluentAssertions;
using RemittanceManager.Infrastructure.Security;

namespace RemittanceManager.Infrastructure.Tests.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_ReturnsNonEmptyString_DifferentFromOriginalPassword()
    {
        var hash = _sut.Hash("Secret123!");

        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe("Secret123!");
    }

    [Fact]
    public void Hash_ProducesDifferentHashesForSamePassword_DueToSalt()
    {
        var h1 = _sut.Hash("Secret123!");
        var h2 = _sut.Hash("Secret123!");

        h1.Should().NotBe(h2);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _sut.Hash("Secret123!");

        _sut.Verify("Secret123!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _sut.Hash("Secret123!");

        _sut.Verify("WrongPass", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithNullOrEmpty_ReturnsFalse()
    {
        var hash = _sut.Hash("Secret123!");

        _sut.Verify(null!, hash).Should().BeFalse();
        _sut.Verify("", hash).Should().BeFalse();
        _sut.Verify("Secret123!", "").Should().BeFalse();
    }
}
