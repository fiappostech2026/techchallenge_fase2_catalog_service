using FCG.Catalog.Domain.Dto;

namespace FCG.Catalog.Domain.Interfaces;

public interface ISearchService
{
    Task IndexAsync(GameResponse game);
    Task RemoveAsync(Guid id);
    Task ReindexAsync(IEnumerable<GameResponse> games);
    Task<IReadOnlyList<GameSearchResult>> SearchAsync(string term, int size);
}
