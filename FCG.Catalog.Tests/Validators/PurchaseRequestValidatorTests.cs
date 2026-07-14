using FCG.Catalog.Domain.Dto;
using FCG.Catalog.Domain.Validators;
using Xunit;

namespace FCG.Catalog.Tests.Validators;

public sealed class PurchaseRequestValidatorTests
{
    private readonly PurchaseRequestValidator _sut = new();

    [Fact]
    public void PurchaseRequestValidator_TwoNonEmptyGuids_ValidationPasses()
    {
        var request = new PurchaseRequest { UserId = Guid.NewGuid(), GameId = Guid.NewGuid() };

        var result = _sut.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void PurchaseRequestValidator_EmptyUserId_Fails_WithUserIdError()
    {
        var request = new PurchaseRequest { UserId = Guid.Empty, GameId = Guid.NewGuid() };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PurchaseRequest.UserId));
    }

    [Fact]
    public void PurchaseRequestValidator_BothGuidsEmpty_FailsWithAtLeastTwoErrors()
    {
        var request = new PurchaseRequest { UserId = Guid.Empty, GameId = Guid.Empty };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 2);
    }
}
