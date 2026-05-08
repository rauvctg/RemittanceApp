using FluentAssertions;
using Moq;
using RemittanceManager.Application.Abstractions.Time;
using RemittanceManager.Application.Common.Exceptions;
using RemittanceManager.Application.Remittances;
using RemittanceManager.Application.Remittances.Dtos;
using RemittanceManager.Domain.Entities;
using RemittanceManager.Domain.Enums;
using RemittanceManager.Domain.Repositories;

namespace RemittanceManager.Application.Tests.Remittances;

public class RemittanceServiceTests
{
    private readonly Mock<IRemittanceRepository> _repo = new();
    private readonly Mock<IClock> _clock = new();
    private readonly DateTime _now = new(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _userId = Guid.NewGuid();

    public RemittanceServiceTests()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(_now);
    }

    private RemittanceService CreateSut()
    {
        return new RemittanceService(_repo.Object, _clock.Object);
    }

    private static CreateRemittanceRequest ValidCreate()
    {
        return new CreateRemittanceRequest
        {
            SenderName = "Juan Perez",
            SenderIdType = IdDocumentType.CC,
            SenderIdNumber = "12345678",
            BeneficiaryName = "Carlos Useche",
            BeneficiaryIdType = IdDocumentType.CC,
            BeneficiaryIdNumber = "87654321",
            Amount = 250.50m,
            Currency = "USD"
        };
    }

    private static UpdateRemittanceRequest ValidUpdate()
    {
        return new UpdateRemittanceRequest
        {
            SenderName = "Juan P.",
            SenderIdType = IdDocumentType.CE,
            SenderIdNumber = "1234567",
            BeneficiaryName = "Carlos U.",
            BeneficiaryIdType = IdDocumentType.CE,
            BeneficiaryIdNumber = "7654321",
            Amount = 300m,
            Currency = "USD",
            Status = RemittanceStatus.Sent
        };
    }

    [Fact]
    public async Task Create_PersistsRemittanceForCurrentUser_AndReturnsDto()
    {
        var request = ValidCreate();
        var sut = CreateSut();

        var result = await sut.CreateAsync(_userId, request, CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        result.UserId.Should().Be(_userId);
        result.SenderName.Should().Be("Juan Perez");
        result.SenderIdType.Should().Be(IdDocumentType.CC);
        result.SenderIdNumber.Should().Be("12345678");
        result.BeneficiaryName.Should().Be("Carlos Useche");
        result.BeneficiaryIdType.Should().Be(IdDocumentType.CC);
        result.BeneficiaryIdNumber.Should().Be("87654321");
        result.Amount.Should().Be(250.50m);
        result.Currency.Should().Be("USD");
        result.Status.Should().Be(RemittanceStatus.Pending);
        result.Date.Should().Be(DateOnly.FromDateTime(_now));
        result.CreatedAtUtc.Should().Be(_now);

        _repo.Verify(r => r.AddAsync(
            It.Is<Remittance>(x =>
                x.Id == result.Id &&
                x.UserId == _userId &&
                x.SenderName == "Juan Perez" &&
                x.BeneficiaryName == "Carlos Useche" &&
                x.Amount == 250.50m &&
                x.Currency == "USD" &&
                x.Status == RemittanceStatus.Pending &&
                x.CreatedAtUtc == _now),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_NormalizesCurrencyToUppercaseAndTrimsBeneficiary()
    {
        var request = new CreateRemittanceRequest { SenderName = "Ana", SenderIdType = IdDocumentType.CC, SenderIdNumber = "123456", BeneficiaryName = "  Carlos  ", BeneficiaryIdType = IdDocumentType.CC, BeneficiaryIdNumber = "654321", Amount = 100m, Currency = "  usd " };
        var sut = CreateSut();

        var result = await sut.CreateAsync(_userId, request, CancellationToken.None);

        result.Currency.Should().Be("USD");
        result.BeneficiaryName.Should().Be("Carlos");
    }

    [Theory]
    [InlineData(IdDocumentType.CC, "123")]
    [InlineData(IdDocumentType.CC, "12345678901")]
    [InlineData(IdDocumentType.CC, "ABC123")]
    [InlineData(IdDocumentType.CE, "12345678")]
    [InlineData(IdDocumentType.DNI, "123")]
    [InlineData(IdDocumentType.Passport, "AB!@")]
    public async Task Create_WithInvalidIdNumber_ThrowsValidation(IdDocumentType type, string invalidNumber)
    {
        var request = new CreateRemittanceRequest
        {
            SenderName = "Ana",
            SenderIdType = type,
            SenderIdNumber = invalidNumber,
            BeneficiaryName = "Carlos",
            BeneficiaryIdType = IdDocumentType.CC,
            BeneficiaryIdNumber = "123456",
            Amount = 100m,
            Currency = "USD"
        };

        var sut = CreateSut();

        var act = async () => await sut.CreateAsync(_userId, request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _repo.Verify(r => r.AddAsync(It.IsAny<Remittance>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetById_WhenExistsForUser_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var entity = new Remittance
        {
            Id = id,
            UserId = _userId,
            BeneficiaryName = "Carlos",
            Amount = 100m,
            Currency = "USD",
            Status = RemittanceStatus.Pending,
            Date = new DateOnly(2026, 5, 1),
            CreatedAtUtc = _now
        };
        _repo.Setup(r => r.GetByIdAsync(id, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var sut = CreateSut();

        var result = await sut.GetByIdAsync(_userId, id, CancellationToken.None);

        result.Id.Should().Be(id);
        result.UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task GetById_WhenNotFoundOrBelongsToAnotherUser_ThrowsNotFound()
    {
        // Repo enforces ownership: returns null when the row doesn't belong to userId.
        _repo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Remittance?)null);

        var sut = CreateSut();

        var act = async () => await sut.GetByIdAsync(_userId, Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task List_ReturnsOnlyCurrentUserRemittances()
    {
        var items = new List<Remittance>
        {
            new() { Id = Guid.NewGuid(), UserId = _userId, BeneficiaryName = "A", Currency = "USD" },
            new() { Id = Guid.NewGuid(), UserId = _userId, BeneficiaryName = "B", Currency = "USD" }
        };
        _repo.Setup(r => r.ListByUserAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var sut = CreateSut();

        var result = await sut.ListAsync(_userId, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.UserId).Should().AllBeEquivalentTo(_userId);
    }

    [Fact]
    public async Task Update_WhenExistsForUser_PersistsAndReturnsUpdatedDto()
    {
        var id = Guid.NewGuid();
        var existing = new Remittance
        {
            Id = id,
            UserId = _userId,
            BeneficiaryName = "Old",
            Amount = 50m,
            Currency = "USD",
            Status = RemittanceStatus.Pending,
            Date = new DateOnly(2026, 4, 1),
            CreatedAtUtc = _now.AddDays(-10)
        };
        _repo.Setup(r => r.GetByIdAsync(id, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Remittance>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateSut();

        var result = await sut.UpdateAsync(_userId, id, ValidUpdate(), CancellationToken.None);

        result.BeneficiaryName.Should().Be("Carlos U.");
        result.Amount.Should().Be(300m);
        result.Status.Should().Be(RemittanceStatus.Sent);
        result.Date.Should().Be(new DateOnly(2026, 4, 1), "Date is immutable on update");
        result.CreatedAtUtc.Should().Be(_now.AddDays(-10), "CreatedAt must not be overwritten");
        result.UpdatedAtUtc.Should().Be(_now);

        _repo.Verify(r => r.UpdateAsync(
            It.Is<Remittance>(x => x.Id == id && x.UserId == _userId && x.BeneficiaryName == "Carlos U." && x.Amount == 300m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WhenNotFoundOrNotOwned_ThrowsNotFoundAndDoesNotPersist()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((Remittance?)null);

        var sut = CreateSut();

        var act = async () => await sut.UpdateAsync(_userId, Guid.NewGuid(), ValidUpdate(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Remittance>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(RemittanceStatus.Pending, RemittanceStatus.Completed)]
    [InlineData(RemittanceStatus.Sent, RemittanceStatus.Pending)]
    [InlineData(RemittanceStatus.Completed, RemittanceStatus.Sent)]
    [InlineData(RemittanceStatus.Cancelled, RemittanceStatus.Pending)]
    public async Task Update_WithInvalidStatusTransition_ThrowsConflictAndDoesNotPersist(RemittanceStatus from, RemittanceStatus to)
    {
        var id = Guid.NewGuid();
        var existing = new Remittance { Id = id, UserId = _userId, Status = from, Currency = "USD", BeneficiaryName = "B", SenderName = "S" };
        _repo.Setup(r => r.GetByIdAsync(id, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var request = ValidUpdate();
        request.Status = to;

        var sut = CreateSut();

        var act = async () => await sut.UpdateAsync(_userId, id, request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Invalid status transition*");
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Remittance>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenStatusIsNotPending_OnlyAllowsStatusChangeAndIgnoresOtherFields()
    {
        var id = Guid.NewGuid();
        var existing = new Remittance
        {
            Id = id,
            UserId = _userId,
            SenderName = "Original Sender",
            SenderIdType = IdDocumentType.CC,
            SenderIdNumber = "1",
            BeneficiaryName = "Original Beneficiary",
            BeneficiaryIdType = IdDocumentType.CC,
            BeneficiaryIdNumber = "2",
            Amount = 100m,
            Currency = "USD",
            Status = RemittanceStatus.Sent,
            Date = new DateOnly(2026, 4, 1),
            CreatedAtUtc = _now.AddDays(-5)
        };
        _repo.Setup(r => r.GetByIdAsync(id, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Remittance>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var request = new UpdateRemittanceRequest
        {
            SenderName = existing.SenderName,
            SenderIdType = existing.SenderIdType,
            SenderIdNumber = existing.SenderIdNumber,
            BeneficiaryName = existing.BeneficiaryName,
            BeneficiaryIdType = existing.BeneficiaryIdType,
            BeneficiaryIdNumber = existing.BeneficiaryIdNumber,
            Amount = existing.Amount,
            Currency = existing.Currency,
            Status = RemittanceStatus.Completed
        };

        var sut = CreateSut();
        var result = await sut.UpdateAsync(_userId, id, request, CancellationToken.None);

        result.Status.Should().Be(RemittanceStatus.Completed);
        result.Amount.Should().Be(100m);
        result.UpdatedAtUtc.Should().Be(_now);
    }

    [Fact]
    public async Task Update_WhenStatusIsNotPending_AndBusinessFieldChanges_ThrowsConflict()
    {
        var id = Guid.NewGuid();
        var existing = new Remittance
        {
            Id = id,
            UserId = _userId,
            SenderName = "Original",
            SenderIdType = IdDocumentType.CC,
            SenderIdNumber = "1",
            BeneficiaryName = "Original",
            BeneficiaryIdType = IdDocumentType.CC,
            BeneficiaryIdNumber = "2",
            Amount = 100m,
            Currency = "USD",
            Status = RemittanceStatus.Sent,
            Date = new DateOnly(2026, 4, 1)
        };
        _repo.Setup(r => r.GetByIdAsync(id, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var request = new UpdateRemittanceRequest
        {
            SenderName = existing.SenderName,
            SenderIdType = existing.SenderIdType,
            SenderIdNumber = existing.SenderIdNumber,
            BeneficiaryName = existing.BeneficiaryName,
            BeneficiaryIdType = existing.BeneficiaryIdType,
            BeneficiaryIdNumber = existing.BeneficiaryIdNumber,
            Amount = 999m,
            Currency = existing.Currency,
            Status = RemittanceStatus.Completed
        };

        var sut = CreateSut();

        var act = async () => await sut.UpdateAsync(_userId, id, request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Cannot modify fields*");
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Remittance>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(RemittanceStatus.Pending)]
    [InlineData(RemittanceStatus.Cancelled)]
    public async Task Delete_WhenStatusAllowsIt_DeletesIt(RemittanceStatus status)
    {
        var id = Guid.NewGuid();
        var existing = new Remittance { Id = id, UserId = _userId, Status = status, Currency = "USD", BeneficiaryName = "B", SenderName = "S" };
        _repo.Setup(r => r.GetByIdAsync(id, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repo.Setup(r => r.DeleteAsync(id, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateSut();

        await sut.DeleteAsync(_userId, id, CancellationToken.None);

        _repo.Verify(r => r.DeleteAsync(id, _userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(RemittanceStatus.Sent)]
    [InlineData(RemittanceStatus.Completed)]
    public async Task Delete_WhenStatusForbidsIt_ThrowsConflictAndDoesNotPersist(RemittanceStatus status)
    {
        var id = Guid.NewGuid();
        var existing = new Remittance { Id = id, UserId = _userId, Status = status, Currency = "USD", BeneficiaryName = "B", SenderName = "S" };
        _repo.Setup(r => r.GetByIdAsync(id, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var sut = CreateSut();

        var act = async () => await sut.DeleteAsync(_userId, id, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Cannot delete*");
        _repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WhenNotFoundOrNotOwned_ThrowsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((Remittance?)null);

        var sut = CreateSut();

        var act = async () => await sut.DeleteAsync(_userId, Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByTrackingCode_WhenExists_ReturnsTrackingResult()
    {
        var entity = new Remittance
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            BeneficiaryName = "Maria Garcia",
            Amount = 250m,
            Currency = "USD",
            Status = RemittanceStatus.Sent,
            Date = new DateOnly(2026, 5, 1),
            CreatedAtUtc = _now,
            TrackingCode = "RM-ABC123"
        };

        _repo.Setup(r => r.GetByTrackingCodeAsync("RM-ABC123", It.IsAny<CancellationToken>()))
             .ReturnsAsync(entity);

        var sut = CreateSut();
        var result = await sut.GetByTrackingCodeAsync("RM-ABC123", CancellationToken.None);

        result.TrackingCode.Should().Be("RM-ABC123");
        result.BeneficiaryName.Should().Be("Maria Garcia");
        result.Amount.Should().Be(250m);
        result.Currency.Should().Be("USD");
        result.Status.Should().Be(RemittanceStatus.Sent);
    }

    [Fact]
    public async Task GetByTrackingCode_NormalizesCodeToUppercase()
    {
        var entity = new Remittance
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            BeneficiaryName = "Carlos",
            Amount = 100m,
            Currency = "EUR",
            Status = RemittanceStatus.Pending,
            Date = new DateOnly(2026, 5, 1),
            CreatedAtUtc = _now,
            TrackingCode = "RM-ABCDEF"
        };

        _repo.Setup(r => r.GetByTrackingCodeAsync("RM-ABCDEF", It.IsAny<CancellationToken>()))
             .ReturnsAsync(entity);

        var sut = CreateSut();
        var result = await sut.GetByTrackingCodeAsync("rm-abcdef", CancellationToken.None);

        result.TrackingCode.Should().Be("RM-ABCDEF");
    }

    [Fact]
    public async Task GetByTrackingCode_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByTrackingCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Remittance?)null);

        var sut = CreateSut();
        var result = await sut.GetByTrackingCodeAsync("RM-NOTFOUND", CancellationToken.None);

        result.Should().BeNull();
    }
}
