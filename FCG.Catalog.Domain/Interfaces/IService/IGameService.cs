using FCG.Catalog.Domain.Dto;

namespace FCG.Catalog.Domain.Interfaces.IService;

public interface IGameService
{
    Task<GameResponse> CreateAsync(GameRequest request);
    Task<GameResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<GameResponse>> GetAllAsync();
    Task<GameResponse?> UpdateAsync(Guid id, GameRequest request);
    Task<bool> DeleteAsync(Guid id);
}
