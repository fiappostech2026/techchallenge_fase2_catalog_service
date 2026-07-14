using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Interfaces.IRepository;
using FCG.Catalog.Domain.Interfaces.IService;

namespace FCG.Catalog.Domain.Services;

public sealed class LibraryService : ILibraryService
{
    private readonly ILibraryRepository _libraryRepository;

    public LibraryService(ILibraryRepository libraryRepository)
    {
        ArgumentNullException.ThrowIfNull(libraryRepository);

        _libraryRepository = libraryRepository;
    }

    public async Task<bool> AddToLibraryAsync(Guid userId, Guid gameId)
    {
        var alreadyOwned = await _libraryRepository.ExistsAsync(userId, gameId);
        if (alreadyOwned)
            return false;

        var item = new LibraryItem { UserId = userId, GameId = gameId };

        await _libraryRepository.AdicionarAsync(item);
        await _libraryRepository.SalvarAlteracoesAsync();

        return true;
    }
}
