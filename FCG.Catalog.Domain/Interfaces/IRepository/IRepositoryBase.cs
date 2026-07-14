namespace FCG.Catalog.Domain.Interfaces.IRepository;

public interface IRepositoryBase<T> where T : class
{
    Task<T?> ObterPorIdAsync(Guid id);
    Task<IEnumerable<T>> ObterTodosAsync();
    Task AdicionarAsync(T entidade);
    Task AtualizarAsync(T entidade);
    Task ExcluirAsync(T entidade);
    Task SalvarAlteracoesAsync();
}
