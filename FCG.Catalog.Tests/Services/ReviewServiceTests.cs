using FCG.Catalog.Domain.Dto;
using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Interfaces.IRepository;
using FCG.Catalog.Domain.Services;
using Moq;
using Xunit;

namespace FCG.Catalog.Tests.Services;

public sealed class ReviewServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsAndReturnsEchoedResponse()
    {
        var repository = new Mock<IReviewRepository>();
        var sut = new ReviewService(repository.Object);
        var gameId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new ReviewRequest { Rating = 5, Comment = "Otimo jogo" };

        var result = await sut.CreateAsync(gameId, userId, "Jogador", request);

        repository.Verify(r => r.AdicionarAsync(It.IsAny<Review>()), Times.Once);
        Assert.Equal(gameId, result.GameId);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("Jogador", result.UserName);
        Assert.Equal(request.Rating, result.Rating);
        Assert.Equal(request.Comment, result.Comment);
    }

    [Fact]
    public async Task CreateAsync_NullRequest_ThrowsArgumentNullException_RepositoryUntouched()
    {
        var repository = new Mock<IReviewRepository>();
        var sut = new ReviewService(repository.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.CreateAsync(Guid.NewGuid(), Guid.NewGuid(), "Jogador", null!));

        repository.Verify(r => r.AdicionarAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task GetByGameIdAsync_ReturnsMappedReviews()
    {
        var gameId = Guid.NewGuid();
        var reviews = new List<Review>
        {
            new() { GameId = gameId, Rating = 4, Comment = "Bom" },
            new() { GameId = gameId, Rating = 2, Comment = "Regular" }
        };
        var repository = new Mock<IReviewRepository>();
        repository.Setup(r => r.ObterPorJogoAsync(gameId)).ReturnsAsync(reviews);
        var sut = new ReviewService(repository.Object);

        var result = (await sut.GetByGameIdAsync(gameId)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(gameId, r.GameId));
    }

    [Fact]
    public async Task GetByGameIdAsync_NoReviews_ReturnsEmpty()
    {
        var repository = new Mock<IReviewRepository>();
        repository.Setup(r => r.ObterPorJogoAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Review>());
        var sut = new ReviewService(repository.Object);

        var result = await sut.GetByGameIdAsync(Guid.NewGuid());

        Assert.Empty(result);
    }
}
