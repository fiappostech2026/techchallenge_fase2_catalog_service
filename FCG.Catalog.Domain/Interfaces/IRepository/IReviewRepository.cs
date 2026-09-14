using FCG.Catalog.Domain.Entities;

namespace FCG.Catalog.Domain.Interfaces.IRepository;

public interface IReviewRepository
{
    Task AdicionarAsync(Review review);
    Task<IEnumerable<Review>> ObterPorJogoAsync(Guid gameId);
}
