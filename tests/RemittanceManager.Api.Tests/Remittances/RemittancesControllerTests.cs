using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RemittanceManager.Api.Auth;
using RemittanceManager.Api.Controllers;
using RemittanceManager.Application.Remittances;
using RemittanceManager.Application.Remittances.Dtos;
using RemittanceManager.Domain.Enums;

namespace RemittanceManager.Api.Tests.Remittances;

public class RemittancesControllerTests
{
    private readonly Mock<IRemittanceService> _service = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _userId = Guid.NewGuid();

    public RemittancesControllerTests()
    {
        _currentUser.Setup(c => c.GetUserId()).Returns(_userId);
    }

    private RemittancesController CreateSut()
    {
        return new RemittancesController(_service.Object, _currentUser.Object);
    }

    private RemittanceDto BuildDto(Guid? id = null)
    {
        return new RemittanceDto
        {
            Id = id ?? Guid.NewGuid(),
            UserId = _userId,
            BeneficiaryName = "Maria Garcia",
            Amount = 250m,
            Currency = "USD",
            Status = RemittanceStatus.Pending,
            Date = new DateOnly(2026, 5, 1),
            CreatedAtUtc = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc)
        };
    }

    [Fact]
    public async Task List_Returns200WithCallerRemittances()
    {
        // Arrange
        var items = new List<RemittanceDto> { BuildDto(), BuildDto() };
        _service.Setup(s => s.ListAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(items);

        // Act
        var result = await CreateSut().List(CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(items);
    }

    [Fact]
    public async Task List_PassesCallerUserIdToService()
    {
        // Arrange
        _service.Setup(s => s.ListAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<RemittanceDto>());

        // Act
        await CreateSut().List(CancellationToken.None);

        // Assert
        _service.Verify(s => s.ListAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
        _currentUser.Verify(c => c.GetUserId(), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenFound_Returns200WithDto()
    {
        // Arrange
        var dto = BuildDto();
        _service.Setup(s => s.GetByIdAsync(_userId, dto.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(dto);

        // Act
        var result = await CreateSut().GetById(dto.Id, CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetById_PassesCallerUserIdAndRemittanceIdToService()
    {
        // Arrange
        var remittanceId = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(_userId, remittanceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildDto(remittanceId));

        // Act
        await CreateSut().GetById(remittanceId, CancellationToken.None);

        // Assert
        _service.Verify(s => s.GetByIdAsync(_userId, remittanceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithValidRequest_Returns201WithLocationAndBody()
    {
        // Arrange
        var dto = BuildDto();
        var request = new CreateRemittanceRequest
        {
            BeneficiaryName = dto.BeneficiaryName,
            Amount = dto.Amount,
            Currency = dto.Currency,
            Date = dto.Date
        };

        _service.Setup(s => s.CreateAsync(_userId, request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(dto);

        // Act
        var result = await CreateSut().Create(request, CancellationToken.None);

        // Assert
        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().Be(dto);
        created.ActionName.Should().Be(nameof(RemittancesController.GetById));
        created.RouteValues!["id"].Should().Be(dto.Id);
    }

    [Fact]
    public async Task Create_PassesCallerUserIdToService()
    {
        // Arrange
        var dto = BuildDto();
        var request = new CreateRemittanceRequest
        {
            BeneficiaryName = "X",
            Amount = 10m,
            Currency = "USD",
            Date = DateOnly.FromDateTime(DateTime.Today)
        };
        _service.Setup(s => s.CreateAsync(_userId, request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(dto);

        // Act
        await CreateSut().Create(request, CancellationToken.None);

        // Assert
        _service.Verify(s => s.CreateAsync(_userId, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WhenFound_Returns200WithUpdatedDto()
    {
        // Arrange
        var dto = BuildDto();
        var request = new UpdateRemittanceRequest
        {
            BeneficiaryName = "Updated Name",
            Amount = 300m,
            Currency = "EUR",
            Date = dto.Date,
            Status = RemittanceStatus.Sent
        };

        _service.Setup(s => s.UpdateAsync(_userId, dto.Id, request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(dto);

        // Act
        var result = await CreateSut().Update(dto.Id, request, CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Update_PassesCallerUserIdAndRemittanceIdToService()
    {
        // Arrange
        var dto = BuildDto();
        var request = new UpdateRemittanceRequest
        {
            BeneficiaryName = "Y",
            Amount = 50m,
            Currency = "USD",
            Date = DateOnly.FromDateTime(DateTime.Today),
            Status = RemittanceStatus.Completed
        };
        _service.Setup(s => s.UpdateAsync(_userId, dto.Id, request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(dto);

        // Act
        await CreateSut().Update(dto.Id, request, CancellationToken.None);

        // Assert
        _service.Verify(s => s.UpdateAsync(_userId, dto.Id, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenFound_Returns204NoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        _service.Setup(s => s.DeleteAsync(_userId, id, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        // Act
        var result = await CreateSut().Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>()
              .Which.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task Delete_PassesCallerUserIdAndRemittanceIdToService()
    {
        // Arrange
        var id = Guid.NewGuid();
        _service.Setup(s => s.DeleteAsync(_userId, id, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        // Act
        await CreateSut().Delete(id, CancellationToken.None);

        // Assert
        _service.Verify(s => s.DeleteAsync(_userId, id, It.IsAny<CancellationToken>()), Times.Once);
        _currentUser.Verify(c => c.GetUserId(), Times.Once);
    }
}
