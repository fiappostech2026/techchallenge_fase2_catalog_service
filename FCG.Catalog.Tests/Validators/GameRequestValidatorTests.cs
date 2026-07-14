using FCG.Catalog.Domain.Dto;
using FCG.Catalog.Domain.Validators;
using Xunit;

namespace FCG.Catalog.Tests.Validators;

public sealed class GameRequestValidatorTests
{
    private readonly GameRequestValidator _sut = new();

    [Fact]
    public void GameRequestValidator_ValidRequest_ValidationPasses()
    {
        var request = new GameRequest { Title = "Hades", Price = 1m };

        var result = _sut.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GameRequestValidator_EmptyTitle_Fails_WithTitleError()
    {
        var request = new GameRequest { Title = string.Empty, Price = 1m };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GameRequest.Title));
    }

    [Fact]
    public void GameRequestValidator_NegativePrice_Fails_WithPriceError()
    {
        var request = new GameRequest { Title = "Hades", Price = -1m };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GameRequest.Price));
    }

    [Fact]
    public void GameRequestValidator_GenreExceedsMaxLength_Fails_WithGenreError()
    {
        var request = new GameRequest { Title = "Hades", Price = 1m, Genre = new string('a', 101) };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GameRequest.Genre));
    }

    [Fact]
    public void GameRequestValidator_PriceAtMaximumAllowed_ValidationPasses()
    {
        var request = new GameRequest { Title = "Hades", Price = 99999999.99m };

        var result = _sut.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GameRequestValidator_PriceAboveMaximumAllowed_Fails_WithPriceError()
    {
        var request = new GameRequest { Title = "Hades", Price = 100000000.00m };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GameRequest.Price));
    }
}
