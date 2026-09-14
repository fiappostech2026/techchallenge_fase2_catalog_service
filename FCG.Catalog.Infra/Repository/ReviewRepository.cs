using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Interfaces.IRepository;
using MongoDB.Driver;

namespace FCG.Catalog.Infra.Repository;

public sealed class ReviewRepository : IReviewRepository
{
    private readonly IMongoCollection<Review> _reviews;

    public ReviewRepository(IMongoDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);

        _reviews = database.GetCollection<Review>("reviews");
    }

    public Task AdicionarAsync(Review review) => _reviews.InsertOneAsync(review);

    public async Task<IEnumerable<Review>> ObterPorJogoAsync(Guid gameId)
    {
        var cursor = await _reviews.FindAsync(r => r.GameId == gameId);
        return await cursor.ToListAsync();
    }
}
