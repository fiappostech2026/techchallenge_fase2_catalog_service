using FCG.Catalog.Domain.Interfaces.IRepository;
using FCG.Catalog.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace FCG.Catalog.Infra.Repository;

public class RepositoryBase<T> : IRepositoryBase<T> where T : class
{
    protected readonly CatalogContext _context;
    protected readonly DbSet<T> _dbSet;

    public RepositoryBase(CatalogContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> ObterPorIdAsync(Guid id) => await _dbSet.FindAsync(id);
    public async Task<IEnumerable<T>> ObterTodosAsync() => await _dbSet.ToListAsync();
    public async Task AdicionarAsync(T entidade) => await _dbSet.AddAsync(entidade);
    public async Task AtualizarAsync(T entidade) => _dbSet.Update(entidade);
    public async Task ExcluirAsync(T entidade) => _dbSet.Remove(entidade);
    public async Task SalvarAlteracoesAsync() => await _context.SaveChangesAsync();
}
