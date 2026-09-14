using FCG.Catalog.Domain.Dto;
using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Interfaces.IRepository;
using FCG.Catalog.Domain.Interfaces.IService;

namespace FCG.Catalog.Domain.Services;

public sealed class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;

    public ReviewService(IReviewRepository reviewRepository)
    {
        ArgumentNullException.ThrowIfNull(reviewRepository);

        _reviewRepository = reviewRepository;
    }

    private static ReviewResponse MapToResponse(Review review) => new()
    {
        Id = review.Id,
        GameId = review.GameId,
        UserId = review.UserId,
        UserName = review.UserName,
        Rating = review.Rating,
        Comment = review.Comment,
        CreatedAt = review.CreatedAt
    };

    public async Task<ReviewResponse> CreateAsync(Guid gameId, Guid userId, string userName, ReviewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var review = new Review
        {
            GameId = gameId,
            UserId = userId,
            UserName = userName,
            Rating = request.Rating,
            Comment = request.Comment
        };

        await _reviewRepository.AdicionarAsync(review);

        return MapToResponse(review);
    }

    public async Task<IEnumerable<ReviewResponse>> GetByGameIdAsync(Guid gameId)
    {
        var reviews = await _reviewRepository.ObterPorJogoAsync(gameId);

        return reviews.Select(MapToResponse);
    }
}
