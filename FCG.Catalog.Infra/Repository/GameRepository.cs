using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Interfaces.IRepository;
using FCG.Catalog.Infra.Context;

namespace FCG.Catalog.Infra.Repository;

public sealed class GameRepository : RepositoryBase<Game>, IGameRepository
{
    public GameRepository(CatalogContext context) : base(context) { }
}
