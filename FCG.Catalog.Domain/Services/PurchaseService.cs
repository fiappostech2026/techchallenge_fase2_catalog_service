using FCG.Catalog.Domain.Dto;
using FCG.Catalog.Domain.Interfaces.IRepository;
using FCG.Catalog.Domain.Interfaces.IService;

namespace FCG.Catalog.Domain.Services;

public sealed class PurchaseService : IPurchaseService
{
    private readonly IGameRepository _gameRepository;

    public PurchaseService(IGameRepository gameRepository)
    {
        ArgumentNullException.ThrowIfNull(gameRepository);

        _gameRepository = gameRepository;
    }

    public async Task<OrderPlacedEvent?> CreateOrderAsync(PurchaseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var game = await _gameRepository.ObterPorIdAsync(request.GameId);
        if (game is null)
            return null;

        return new OrderPlacedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = request.UserId,
            GameId = request.GameId,
            Price = game.Price
        };
    }
}
