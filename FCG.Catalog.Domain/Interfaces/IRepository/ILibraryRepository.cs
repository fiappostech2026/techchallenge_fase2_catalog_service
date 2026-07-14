using FCG.Catalog.Domain.Entities;

namespace FCG.Catalog.Domain.Interfaces.IRepository;

public interface ILibraryRepository : IRepositoryBase<LibraryItem>
{
    Task<bool> ExistsAsync(Guid userId, Guid gameId);
    Task<IEnumerable<LibraryItem>> ObterPorUsuarioAsync(Guid userId);
}
