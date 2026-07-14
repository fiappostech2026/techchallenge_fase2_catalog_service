using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Interfaces.IRepository;
using FCG.Catalog.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace FCG.Catalog.Infra.Repository;

public sealed class LibraryRepository : RepositoryBase<LibraryItem>, ILibraryRepository
{
    public LibraryRepository(CatalogContext context) : base(context) { }

    public async Task<bool> ExistsAsync(Guid userId, Guid gameId) =>
        await _dbSet.AnyAsync(item => item.UserId == userId && item.GameId == gameId);

    public async Task<IEnumerable<LibraryItem>> ObterPorUsuarioAsync(Guid userId) =>
        await _dbSet.Where(item => item.UserId == userId).ToListAsync();
}
