using FCG.Catalog.Domain.Dto;
using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Interfaces;
using FCG.Catalog.Domain.Interfaces.IRepository;
using FCG.Catalog.Domain.Interfaces.IService;

namespace FCG.Catalog.Domain.Services;

public sealed class GameService : IGameService
{
    private const string GamesListCacheKey = "catalog:games:all";
    private static readonly TimeSpan GamesListCacheTtl = TimeSpan.FromSeconds(60);
    private const int MaxSearchSize = 50;

    private readonly IGameRepository _gameRepository;
    private readonly ICacheService _cacheService;
    private readonly ISearchService _searchService;

    public GameService(IGameRepository gameRepository, ICacheService cacheService, ISearchService searchService)
    {
        ArgumentNullException.ThrowIfNull(gameRepository);
        ArgumentNullException.ThrowIfNull(cacheService);
        ArgumentNullException.ThrowIfNull(searchService);

        _gameRepository = gameRepository;
        _cacheService = cacheService;
        _searchService = searchService;
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
        await _cacheService.RemoveAsync(GamesListCacheKey);

        var response = MapToResponse(game);
        await _searchService.IndexAsync(response);

        return response;
    }

    public async Task<GameResponse?> GetByIdAsync(Guid id)
    {
        var game = await _gameRepository.ObterPorIdAsync(id);

        return game is null ? null : MapToResponse(game);
    }

    public async Task<IEnumerable<GameResponse>> GetAllAsync()
    {
        var cached = await _cacheService.GetAsync<List<GameResponse>>(GamesListCacheKey);
        if (cached is not null)
            return cached;

        var games = await _gameRepository.ObterTodosAsync();
        var response = games.Select(MapToResponse).ToList();

        await _cacheService.SetAsync(GamesListCacheKey, response, GamesListCacheTtl);

        return response;
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
        await _cacheService.RemoveAsync(GamesListCacheKey);

        var response = MapToResponse(game);
        await _searchService.IndexAsync(response);

        return response;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var game = await _gameRepository.ObterPorIdAsync(id);
        if (game is null)
            return false;

        await _gameRepository.ExcluirAsync(game);
        await _gameRepository.SalvarAlteracoesAsync();
        await _cacheService.RemoveAsync(GamesListCacheKey);
        await _searchService.RemoveAsync(id);

        return true;
    }

    public async Task<int> ReindexAllAsync()
    {
        var games = await _gameRepository.ObterTodosAsync();
        var response = games.Select(MapToResponse).ToList();

        await _searchService.ReindexAsync(response);

        return response.Count;
    }

    public async Task<IReadOnlyList<GameSearchResult>> SearchAsync(string term, int size)
    {
        if (string.IsNullOrWhiteSpace(term))
            return [];

        return await _searchService.SearchAsync(term.Trim(), Math.Clamp(size, 1, MaxSearchSize));
    }
}
