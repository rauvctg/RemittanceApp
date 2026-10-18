using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RemittanceManager.Api.Controllers;
using RemittanceManager.Application.Remittances;
using RemittanceManager.Application.Remittances.Dtos;
using RemittanceManager.Domain.Enums;

namespace RemittanceManager.Api.Tests.Public;

public class PublicControllerTests
{
    private readonly Mock<IRemittanceService> _service = new();

    private PublicController CreateSut()
    {
        return new PublicController(_service.Object);
    }

    [Fact]
    public async Task Track_WhenCodeExists_Returns200WithTrackingResult()
    {
        // Arrange
        var tracking = new TrackingResult
        {
            TrackingCode = "RM-ABC123",
            BeneficiaryName = "Maria Garcia",
            Amount = 250m,
            Currency = "USD",
            Status = RemittanceStatus.Sent,
            Date = new DateOnly(2026, 5, 1)
        };

        _service
            .Setup(s => s.GetByTrackingCodeAsync("RM-ABC123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tracking);

        // Act
        var result = await CreateSut().Track("RM-ABC123", CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().Be(tracking);
    }

    [Fact]
    public async Task Track_DelegatesToRemittanceService_WithGivenCode()
    {
        // Arrange
        _service
            .Setup(s => s.GetByTrackingCodeAsync("RM-XYZ999", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrackingResult { TrackingCode = "RM-XYZ999" });

        // Act
        await CreateSut().Track("RM-XYZ999", CancellationToken.None);

        // Assert
        _service.Verify(s => s.GetByTrackingCodeAsync("RM-XYZ999", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Track_WhenCodeNotFound_Returns404()
    {
        _service
            .Setup(s => s.GetByTrackingCodeAsync("RM-MISSING", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrackingResult?)null);

        var sut = CreateSut();
        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await sut.Track("RM-MISSING", CancellationToken.None);

        result.Result.Should().BeOfType<EmptyResult>();
        sut.Response.StatusCode.Should().Be(404);
    }
}
