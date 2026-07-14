using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Interfaces.IRepository;
using FCG.Catalog.Domain.Services;
using Moq;
using Xunit;

namespace FCG.Catalog.Tests.Services;

public sealed class LibraryServiceTests
{
    [Fact]
    public async Task AddToLibraryAsync_UserDoesNotOwnGame_AddsAndReturnsTrue()
    {
        var repository = new Mock<ILibraryRepository>();
        repository.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);
        var sut = new LibraryService(repository.Object);

        var result = await sut.AddToLibraryAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.True(result);
        repository.Verify(r => r.AdicionarAsync(It.IsAny<LibraryItem>()), Times.Once);
    }

    [Fact]
    public async Task AddToLibraryAsync_UserAlreadyOwnsGame_ReturnsFalse_AdicionarNeverCalled()
    {
        var repository = new Mock<ILibraryRepository>();
        repository.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(true);
        var sut = new LibraryService(repository.Object);

        var result = await sut.AddToLibraryAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result);
        repository.Verify(r => r.AdicionarAsync(It.IsAny<LibraryItem>()), Times.Never);
    }

    [Fact]
    public async Task AddToLibraryAsync_EmptyUserIdNotOwned_StillAddsAndReturnsTrue()
    {
        var repository = new Mock<ILibraryRepository>();
        repository.Setup(r => r.ExistsAsync(Guid.Empty, It.IsAny<Guid>())).ReturnsAsync(false);
        var sut = new LibraryService(repository.Object);

        var result = await sut.AddToLibraryAsync(Guid.Empty, Guid.NewGuid());

        Assert.True(result);
        repository.Verify(r => r.AdicionarAsync(It.IsAny<LibraryItem>()), Times.Once);
    }
}
