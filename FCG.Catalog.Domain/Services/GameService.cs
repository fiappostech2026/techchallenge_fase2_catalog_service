using FCG.Catalog.Domain.Dto;
using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Interfaces.IRepository;
using FCG.Catalog.Domain.Interfaces.IService;

namespace FCG.Catalog.Domain.Services;

public sealed class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;

    public GameService(IGameRepository gameRepository)
    {
        ArgumentNullException.ThrowIfNull(gameRepository);

        _gameRepository = gameRepository;
    }

    private static GameResponse MapToResponse(Game game) => new()
    {
        Id = game.Id,
        Title = game.Title,
        Description = game.Description,
        Price = game.Price,
        Genre = game.Genre
    };

    public async Task<GameResponse> CreateAsync(GameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var game = new Game
        {
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            Genre = request.Genre
        };

        await _gameRepository.AdicionarAsync(game);
        await _gameRepository.SalvarAlteracoesAsync();

        return MapToResponse(game);
    }

    public async Task<GameResponse?> GetByIdAsync(Guid id)
    {
        var game = await _gameRepository.ObterPorIdAsync(id);

        return game is null ? null : MapToResponse(game);
    }

    public async Task<IEnumerable<GameResponse>> GetAllAsync()
    {
        var games = await _gameRepository.ObterTodosAsync();

        return games.Select(MapToResponse);
    }

    public async Task<GameResponse?> UpdateAsync(Guid id, GameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var game = await _gameRepository.ObterPorIdAsync(id);
        if (game is null)
            return null;

        game.Title = request.Title;
        game.Description = request.Description;
        game.Price = request.Price;
        game.Genre = request.Genre;

        await _gameRepository.AtualizarAsync(game);
        await _gameRepository.SalvarAlteracoesAsync();

        return MapToResponse(game);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var game = await _gameRepository.ObterPorIdAsync(id);
        if (game is null)
            return false;

        await _gameRepository.ExcluirAsync(game);
        await _gameRepository.SalvarAlteracoesAsync();

        return true;
    }
}
