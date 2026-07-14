using FCG.Catalog.Domain.Dto;
using FCG.Catalog.Domain.Entities;
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
        var sut = new GameService(repository.Object);
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
        var sut = new GameService(repository.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CreateAsync(null!));

        repository.Verify(r => r.AdicionarAsync(It.IsAny<Game>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_TitleWithFiveThousandCharacters_StillDelegatesToRepository()
    {
        var repository = new Mock<IGameRepository>();
        var sut = new GameService(repository.Object);
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
        var sut = new GameService(repository.Object);
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
        var sut = new GameService(repository.Object);
        var request = new GameRequest { Title = "New", Price = 2m };

        var result = await sut.UpdateAsync(Guid.NewGuid(), request);

        Assert.Null(result);
        repository.Verify(r => r.AtualizarAsync(It.IsAny<Game>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ValidIdNullRequest_ThrowsArgumentNullException()
    {
        var repository = new Mock<IGameRepository>();
        var sut = new GameService(repository.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.UpdateAsync(Guid.NewGuid(), null!));
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesAndReturnsTrue()
    {
        var existing = new Game { Title = "Game" };
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(existing.Id)).ReturnsAsync(existing);
        var sut = new GameService(repository.Object);

        var result = await sut.DeleteAsync(existing.Id);

        Assert.True(result);
        repository.Verify(r => r.ExcluirAsync(existing), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalse_ExcluirNeverCalled()
    {
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync((Game?)null);
        var sut = new GameService(repository.Object);

        var result = await sut.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
        repository.Verify(r => r.ExcluirAsync(It.IsAny<Game>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_EmptyGuid_ReturnsFalse_NoThrow()
    {
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(Guid.Empty)).ReturnsAsync((Game?)null);
        var sut = new GameService(repository.Object);

        var result = await sut.DeleteAsync(Guid.Empty);

        Assert.False(result);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsMatchingResponse()
    {
        var existing = new Game { Title = "Game", Price = 9.99m };
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(existing.Id)).ReturnsAsync(existing);
        var sut = new GameService(repository.Object);

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
        var sut = new GameService(repository.Object);

        var result = await sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyGuid_ReturnsNull_NoThrow()
    {
        var repository = new Mock<IGameRepository>();
        repository.Setup(r => r.ObterPorIdAsync(Guid.Empty)).ReturnsAsync((Game?)null);
        var sut = new GameService(repository.Object);

        var result = await sut.GetByIdAsync(Guid.Empty);

        Assert.Null(result);
    }
}
