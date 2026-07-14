namespace FCG.Catalog.Domain.Interfaces.IService;

public interface ILibraryService
{
    Task<bool> AddToLibraryAsync(Guid userId, Guid gameId);
}
