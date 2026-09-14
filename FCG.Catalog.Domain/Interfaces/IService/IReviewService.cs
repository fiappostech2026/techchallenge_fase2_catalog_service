using FCG.Catalog.Domain.Dto;

namespace FCG.Catalog.Domain.Interfaces.IService;

public interface IReviewService
{
    Task<ReviewResponse> CreateAsync(Guid gameId, Guid userId, string userName, ReviewRequest request);
    Task<IEnumerable<ReviewResponse>> GetByGameIdAsync(Guid gameId);
}
