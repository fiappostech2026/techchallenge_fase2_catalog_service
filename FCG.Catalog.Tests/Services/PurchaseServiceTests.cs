using FCG.Catalog.Domain.Dto;
using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Interfaces.IRepository;
using FCG.Catalog.Domain.Services;
using Moq;
using Xunit;

namespace FCG.Catalog.Tests.Services;

public sealed class PurchaseServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_ExistingGame_ReturnsOrderPlacedEventWithFreshOrderIdAndGamePrice()
    {
        var game = new Game { Title = "Hollow Knight", Price = 49.90m };
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(game.Id)).ReturnsAsync(game);
        var sut = new PurchaseService(repository.Object);
        var request = new PurchaseRequest { UserId = Guid.NewGuid(), GameId = game.Id };

        var result = await sut.CreateOrderAsync(request);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result!.OrderId);
        Assert.Equal(request.UserId, result.UserId);
        Assert.Equal(request.GameId, result.GameId);
        Assert.Equal(49.90m, result.Price);
    }

    [Fact]
    public async Task CreateOrderAsync_GameNotFound_ReturnsNull()
    {
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync((Game?)null);
        var sut = new PurchaseService(repository.Object);
        var request = new PurchaseRequest { UserId = Guid.NewGuid(), GameId = Guid.NewGuid() };

        var result = await sut.CreateOrderAsync(request);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateOrderAsync_NullRequest_ThrowsArgumentNullException()
    {
        var repository = new Mock<IGameRepository>();
        var sut = new PurchaseService(repository.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CreateOrderAsync(null!));
    }
}
