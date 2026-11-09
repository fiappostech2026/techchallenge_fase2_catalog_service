using FCG.Catalog.Domain.Dto;
using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Interfaces;
using FCG.Catalog.Domain.Interfaces.IRepository;
using FCG.Catalog.Domain.Services;
using Moq;
using Xunit;

namespace FCG.Catalog.Tests.Services;

public sealed class GameServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsAndReturnsEchoedResponse()
    {
        var repository = new Mock<IGameRepository>();
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);
        var request = new GameRequest { Title = "Elden Ring", Description = "Action RPG", Price = 59.99m, Genre = "RPG" };

        var result = await sut.CreateAsync(request);

        repository.Verify(r => r.AdicionarAsync(It.IsAny<Game>()), Times.Once);
        repository.Verify(r => r.SalvarAlteracoesAsync(), Times.Once);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Title, result.Title);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(request.Price, result.Price);
        Assert.Equal(request.Genre, result.Genre);
    }

    [Fact]
    public async Task CreateAsync_NullRequest_ThrowsArgumentNullException_RepositoryUntouched()
    {
        var repository = new Mock<IGameRepository>();
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CreateAsync(null!));

        repository.Verify(r => r.AdicionarAsync(It.IsAny<Game>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_TitleWithFiveThousandCharacters_StillDelegatesToRepository()
    {
        var repository = new Mock<IGameRepository>();
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);
        var request = new GameRequest { Title = new string('a', 5000), Description = "desc", Price = 10m, Genre = "Genre" };

        await sut.CreateAsync(request);

        repository.Verify(r => r.AdicionarAsync(It.IsAny<Game>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesAndReturnsResponse()
    {
        var existing = new Game { Title = "Old", Description = "Old desc", Price = 1m, Genre = "Old genre" };
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(existing.Id)).ReturnsAsync(existing);
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);
        var request = new GameRequest { Title = "New", Description = "New desc", Price = 2m, Genre = "New genre" };

        var result = await sut.UpdateAsync(existing.Id, request);

        repository.Verify(r => r.AtualizarAsync(existing), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(request.Title, result!.Title);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsNull_AtualizarNeverCalled()
    {
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync((Game?)null);
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);
        var request = new GameRequest { Title = "New", Price = 2m };

        var result = await sut.UpdateAsync(Guid.NewGuid(), request);

        Assert.Null(result);
        repository.Verify(r => r.AtualizarAsync(It.IsAny<Game>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ValidIdNullRequest_ThrowsArgumentNullException()
    {
        var repository = new Mock<IGameRepository>();
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.UpdateAsync(Guid.NewGuid(), null!));
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesAndReturnsTrue()
    {
        var existing = new Game { Title = "Game" };
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(existing.Id)).ReturnsAsync(existing);
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        var result = await sut.DeleteAsync(existing.Id);

        Assert.True(result);
        repository.Verify(r => r.ExcluirAsync(existing), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalse_ExcluirNeverCalled()
    {
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync((Game?)null);
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        var result = await sut.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
        repository.Verify(r => r.ExcluirAsync(It.IsAny<Game>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_EmptyGuid_ReturnsFalse_NoThrow()
    {
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(Guid.Empty)).ReturnsAsync((Game?)null);
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        var result = await sut.DeleteAsync(Guid.Empty);

        Assert.False(result);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsMatchingResponse()
    {
        var existing = new Game { Title = "Game", Price = 9.99m };
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(existing.Id)).ReturnsAsync(existing);
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        var result = await sut.GetByIdAsync(existing.Id);

        Assert.NotNull(result);
        Assert.Equal(existing.Id, result!.Id);
        Assert.Equal(existing.Title, result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync((Game?)null);
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        var result = await sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyGuid_ReturnsNull_NoThrow()
    {
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(Guid.Empty)).ReturnsAsync((Game?)null);
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        var result = await sut.GetByIdAsync(Guid.Empty);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_CacheHit_ReturnsCachedList_RepositoryNeverCalled()
    {
        var cachedList = new List<GameResponse> { new() { Title = "Cached Game" } };
        var repository = new Mock<IGameRepository>();
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        cache.Setup(c => c.GetAsync<List<GameResponse>>("catalog:games:all")).ReturnsAsync(cachedList);
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        var result = await sut.GetAllAsync();

        Assert.Same(cachedList, result);
        repository.Verify(r => r.ObterTodosAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_CacheMiss_QueriesRepositoryAndPopulatesCache()
    {
        var games = new List<Game> { new() { Title = "Hades" } };
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterTodosAsync()).ReturnsAsync(games);
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        cache.Setup(c => c.GetAsync<List<GameResponse>>("catalog:games:all")).ReturnsAsync((List<GameResponse>?)null);
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        var result = await sut.GetAllAsync();

        Assert.Single(result);
        cache.Verify(c => c.SetAsync("catalog:games:all", It.IsAny<List<GameResponse>>(), TimeSpan.FromSeconds(60)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_InvalidatesGamesListCache()
    {
        var repository = new Mock<IGameRepository>();
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);
        var request = new GameRequest { Title = "New Game", Price = 10m };

        await sut.CreateAsync(request);

        cache.Verify(c => c.RemoveAsync("catalog:games:all"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_InvalidatesGamesListCache()
    {
        var existing = new Game { Title = "Game" };
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(existing.Id)).ReturnsAsync(existing);
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        await sut.DeleteAsync(existing.Id);

        cache.Verify(c => c.RemoveAsync("catalog:games:all"), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_IndexesGameInSearch()
    {
        var repository = new Mock<IGameRepository>();
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);
        var request = new GameRequest { Title = "Hollow Knight", Description = "Metroidvania", Price = 30m, Genre = "Action" };

        var result = await sut.CreateAsync(request);

        search.Verify(s => s.IndexAsync(It.Is<GameResponse>(g => g.Id == result.Id && g.Title == "Hollow Knight")), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_ReindexesUpdatedGame()
    {
        var existing = new Game { Title = "Old" };
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(existing.Id)).ReturnsAsync(existing);
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        await sut.UpdateAsync(existing.Id, new GameRequest { Title = "New", Price = 2m });

        search.Verify(s => s.IndexAsync(It.Is<GameResponse>(g => g.Id == existing.Id && g.Title == "New")), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_DoesNotTouchSearch()
    {
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync((Game?)null);
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        await sut.UpdateAsync(Guid.NewGuid(), new GameRequest { Title = "New", Price = 2m });

        search.Verify(s => s.IndexAsync(It.IsAny<GameResponse>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesGameFromSearch()
    {
        var existing = new Game { Title = "Game" };
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(existing.Id)).ReturnsAsync(existing);
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        await sut.DeleteAsync(existing.Id);

        search.Verify(s => s.RemoveAsync(existing.Id), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_ValidTerm_DelegatesTrimmedTermToSearch()
    {
        var expected = new List<GameSearchResult> { new() { Title = "The Legend of Zelda", Score = 3.2 } };
        var repository = new Mock<IGameRepository>();
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        search.Setup(s => s.SearchAsync("zeldda", 10)).ReturnsAsync(expected);
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        var result = await sut.SearchAsync("  zeldda  ", 10);

        Assert.Same(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_BlankTerm_ReturnsEmpty_SearchNeverCalled(string term)
    {
        var repository = new Mock<IGameRepository>();
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        var result = await sut.SearchAsync(term, 10);

        Assert.Empty(result);
        search.Verify(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(500, 50)]
    public async Task SearchAsync_SizeOutOfRange_IsClamped(int requested, int expected)
    {
        var repository = new Mock<IGameRepository>();
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        search.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(new List<GameSearchResult>());
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        await sut.SearchAsync("mario", requested);

        search.Verify(s => s.SearchAsync("mario", expected), Times.Once);
    }

    [Fact]
    public async Task ReindexAllAsync_SendsAllGamesFromRepositoryToSearch()
    {
        var games = new List<Game> { new() { Title = "Hades" }, new() { Title = "Celeste" } };
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterTodosAsync()).ReturnsAsync(games);
        var cache = new Mock<ICacheService>();
        var search = new Mock<ISearchService>();
        var sut = new GameService(repository.Object, cache.Object, search.Object);

        var total = await sut.ReindexAllAsync();

        Assert.Equal(2, total);
        search.Verify(s => s.ReindexAsync(It.Is<IEnumerable<GameResponse>>(g => g.Count() == 2)), Times.Once);
    }
}
