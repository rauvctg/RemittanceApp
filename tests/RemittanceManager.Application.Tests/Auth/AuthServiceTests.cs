using FluentAssertions;
using Moq;
using RemittanceManager.Application.Abstractions.Security;
using RemittanceManager.Application.Abstractions.Time;
using RemittanceManager.Application.Auth;
using RemittanceManager.Application.Auth.Dtos;
using RemittanceManager.Application.Common.Exceptions;
using RemittanceManager.Domain.Entities;
using RemittanceManager.Domain.Repositories;

namespace RemittanceManager.Application.Tests.Auth;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenGenerator> _tokens = new();
    private readonly Mock<IClock> _clock = new();
    private readonly DateTime _now = new(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc);

    public AuthServiceTests()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(_now);
    }

    private AuthService CreateSut()
    {
        return new AuthService(_users.Object, _hasher.Object, _tokens.Object, _clock.Object);
    }

    [Fact]
    public async Task Register_WithValidData_PersistsUserAndReturnsNewId()
    {
        // Arrange
        var request = new RegisterRequest { Email = "maria@test.com", Password = "Secret123!", DisplayName = "Maria" };

        _users
            .Setup(r => r.GetByEmailAsync("maria@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _hasher.Setup(h => h.Hash("Secret123!")).Returns("HASHED");

        var sut = CreateSut();

        // Act
        var userId = await sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        userId.Should().NotBeEmpty();

        _users.Verify(r => r.AddAsync(
            It.Is<User>(u =>
                u.Id == userId &&
                u.Email == "maria@test.com" &&
                u.DisplayName == "Maria" &&
                u.PasswordHash == "HASHED" &&
                u.CreatedAtUtc == _now),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_WithDuplicatedEmail_ThrowsConflictAndDoesNotPersist()
    {
        // Arrange
        var request = new RegisterRequest { Email = "maria@test.com", Password = "Secret123!", DisplayName = "Maria" };
        var existing = new User { Id = Guid.NewGuid(), Email = "maria@test.com" };

        _users
            .Setup(r => r.GetByEmailAsync("maria@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already*");

        _users.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _hasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Register_NormalizesEmailToLowercaseAndTrims()
    {
        // Arrange
        var request = new RegisterRequest { Email = "  Maria@Test.COM ", Password = "Secret123!", DisplayName = "  Maria  " };

        _users
            .Setup(r => r.GetByEmailAsync("maria@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _hasher.Setup(h => h.Hash("Secret123!")).Returns("HASHED");

        var sut = CreateSut();

        // Act
        await sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        _users.Verify(r => r.GetByEmailAsync("maria@test.com", It.IsAny<CancellationToken>()), Times.Once);
        _users.Verify(r => r.AddAsync(
            It.Is<User>(u => u.Email == "maria@test.com" && u.DisplayName == "Maria"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndProfile()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "maria@test.com",
            DisplayName = "Maria",
            PasswordHash = "HASHED",
            CreatedAtUtc = _now
        };

        _users
            .Setup(r => r.GetByEmailAsync("maria@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _hasher.Setup(h => h.Verify("Secret123!", "HASHED")).Returns(true);
        _tokens.Setup(t => t.Generate(user)).Returns("JWT-TOKEN");

        var sut = CreateSut();

        // Act
        var response = await sut.LoginAsync(new LoginRequest { Email = "Maria@Test.com", Password = "Secret123!" }, CancellationToken.None);

        // Assert
        response.Token.Should().Be("JWT-TOKEN");
        response.UserId.Should().Be(user.Id);
        response.Email.Should().Be("maria@test.com");
        response.DisplayName.Should().Be("Maria");
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ThrowsUnauthorized()
    {
        // Arrange
        _users
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.LoginAsync(new LoginRequest { Email = "ghost@test.com", Password = "whatever" }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
        _hasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _tokens.Verify(t => t.Generate(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorized()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "maria@test.com", PasswordHash = "HASHED" };
        _users
            .Setup(r => r.GetByEmailAsync("maria@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("WRONG", "HASHED")).Returns(false);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.LoginAsync(new LoginRequest { Email = "maria@test.com", Password = "WRONG" }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
        _tokens.Verify(t => t.Generate(It.IsAny<User>()), Times.Never);
    }
}
