using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using FCG.Catalog.Domain.Dto;
using FCG.Catalog.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FCG.Catalog.Infra.Search;

// O SQL Server continua sendo a fonte da verdade; o Elasticsearch é só um índice
// de leitura. Por isso falhas de indexação são logadas e não derrubam a escrita —
// o índice pode ser reconstruído a qualquer momento via ReindexAsync.
public sealed class ElasticsearchService : ISearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchSettings _settings;
    private readonly ILogger<ElasticsearchService> _logger;

    public ElasticsearchService(ElasticsearchClient client, ElasticsearchSettings settings, ILogger<ElasticsearchService> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        _client = client;
        _settings = settings;
        _logger = logger;
    }

    public async Task IndexAsync(GameResponse game)
    {
        var response = await _client.IndexAsync(game, i => i.Index(_settings.IndexName).Id(game.Id.ToString()));

        if (!response.IsValidResponse)
            _logger.LogWarning("Falha ao indexar o jogo {Id} no Elasticsearch: {Erro}", game.Id, response.DebugInformation);
    }

    public async Task RemoveAsync(Guid id)
    {
        var response = await _client.DeleteAsync(_settings.IndexName, id.ToString());

        if (!response.IsValidResponse && response.Result != Result.NotFound)
            _logger.LogWarning("Falha ao remover o jogo {Id} do Elasticsearch: {Erro}", id, response.DebugInformation);
    }

    public async Task ReindexAsync(IEnumerable<GameResponse> games)
    {
        await EnsureIndexAsync();

        var documents = games.ToList();
        if (documents.Count == 0)
            return;

        var response = await _client.BulkAsync(b => b
            .Index(_settings.IndexName)
            .IndexMany(documents, (op, game) => op.Id(game.Id.ToString())));

        if (!response.IsValidResponse || response.Errors)
            throw new InvalidOperationException($"Falha ao reindexar jogos no Elasticsearch: {response.DebugInformation}");
    }

    public async Task<IReadOnlyList<GameSearchResult>> SearchAsync(string term, int size)
    {
        // multi_match + fuzziness AUTO: tolera 1 erro de digitação em termos de 3-5 letras
        // e 2 erros acima disso ("zeldda" encontra "Zelda"). Os boosts (^) fazem um acerto
        // no título pesar mais que no gênero, e no gênero mais que na descrição. Sem sort
        // explícito, o Elasticsearch ordena pelo _score, ou seja, por relevância.
        var response = await _client.SearchAsync<GameResponse>(s => s
            .Indices(_settings.IndexName)
            .Size(size)
            .Query(q => q.MultiMatch(m => m
                .Query(term)
                .Fields(new[] { "title^3", "genre^2", "description" })
                .Fuzziness(new Fuzziness("AUTO"))
                .PrefixLength(1))));

        if (!response.IsValidResponse)
            throw new InvalidOperationException($"Falha na busca no Elasticsearch: {response.DebugInformation}");

        return response.Hits
            .Where(hit => hit.Source is not null)
            .Select(hit => new GameSearchResult
            {
                Id = hit.Source!.Id,
                Title = hit.Source.Title,
                Description = hit.Source.Description,
                Price = hit.Source.Price,
                Genre = hit.Source.Genre,
                Score = hit.Score ?? 0
            })
            .ToList();
    }

    private async Task EnsureIndexAsync()
    {
        var exists = await _client.Indices.ExistsAsync(_settings.IndexName);
        if (exists.Exists)
            return;

        var response = await _client.Indices.CreateAsync<GameResponse>(_settings.IndexName, c => c
            .Mappings(m => m.Properties(p => p
                .Keyword(g => g.Id)
                .Text(g => g.Title)
                .Text(g => g.Description)
                .Text(g => g.Genre)
                .DoubleNumber(g => g.Price))));

        if (!response.IsValidResponse)
            throw new InvalidOperationException($"Falha ao criar o índice {_settings.IndexName}: {response.DebugInformation}");
    }
}
