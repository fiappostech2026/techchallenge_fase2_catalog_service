using FCG.Catalog.Domain.Dto;
using FCG.Catalog.Domain.Enums;
using FCG.Catalog.Domain.Validators;
using Xunit;

namespace FCG.Catalog.Tests.Validators;

public sealed class PaymentProcessedEventValidatorTests
{
    private readonly PaymentProcessedEventValidator _sut = new();

    [Fact]
    public void PaymentProcessedEventValidator_ValidGuidsApprovedStatus_ValidationPasses()
    {
        var evt = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Status = PaymentStatus.Approved
        };

        var result = _sut.Validate(evt);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void PaymentProcessedEventValidator_EmptyOrderId_Fails_WithOrderIdError()
    {
        var evt = new PaymentProcessedEvent
        {
            OrderId = Guid.Empty,
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Status = PaymentStatus.Approved
        };

        var result = _sut.Validate(evt);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PaymentProcessedEvent.OrderId));
    }

    [Fact]
    public void PaymentProcessedEventValidator_OutOfRangeStatus_Fails_WithStatusError()
    {
        var evt = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Status = (PaymentStatus)99
        };

        var result = _sut.Validate(evt);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PaymentProcessedEvent.Status));
    }
}
