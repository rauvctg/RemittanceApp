using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RemittanceManager.Api.Controllers;
using RemittanceManager.Application.Auth;
using RemittanceManager.Application.Auth.Dtos;

namespace RemittanceManager.Api.Tests.Auth;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly Guid _userId = Guid.NewGuid();

    private AuthController CreateSut()
    {
        return new AuthController(_authService.Object);
    }

    [Fact]
    public async Task Register_WithValidRequest_Returns201CreatedAndUserId()
    {
        // Arrange
        var newId = Guid.NewGuid();
        var request = new RegisterRequest
        {
            Email = "maria@test.com",
            Password = "Secret123!",
            DisplayName = "Maria"
        };

        _authService
            .Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newId);

        var sut = CreateSut();

        // Act
        var result = await sut.Register(request, CancellationToken.None);

        // Assert
        var created = result.Should().BeOfType<ObjectResult>().Subject;
        created.StatusCode.Should().Be(201);

        var body = created.Value!.GetType().GetProperty("userId")!.GetValue(created.Value);
        body.Should().Be(newId);

        _authService.Verify(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_DelegatesToAuthService_WithExactRequest()
    {
        // Arrange
        var request = new RegisterRequest { Email = "x@x.com", Password = "P@ss1", DisplayName = "X" };
        _authService
            .Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var sut = CreateSut();

        // Act
        await sut.Register(request, CancellationToken.None);

        // Assert — service called exactly once with the same request object
        _authService.Verify(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithAuthResponse()
    {
        // Arrange
        var request = new LoginRequest { Email = "maria@test.com", Password = "Secret123!" };
        var response = new AuthResponse
        {
            Token = "eyJhbGciOiJIUzI1NiJ9.test",
            UserId = _userId,
            Email = "maria@test.com",
            DisplayName = "Maria"
        };

        _authService
            .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut();

        // Act
        var actionResult = await sut.Login(request, CancellationToken.None);

        // Assert
        var ok = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().Be(response);
    }

    [Fact]
    public async Task Login_DelegatesToAuthService_WithExactRequest()
    {
        // Arrange
        var request = new LoginRequest { Email = "a@b.com", Password = "pw" };
        _authService
            .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResponse());

        var sut = CreateSut();

        // Act
        await sut.Login(request, CancellationToken.None);

        // Assert
        _authService.Verify(s => s.LoginAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
