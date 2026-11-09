namespace FCG.Catalog.Infra.Search;

public sealed class ElasticsearchSettings
{
    public string Url { get; init; } = "http://localhost:9200";
    public string? ApiKey { get; init; }
    public string IndexName { get; init; } = "catalog-games";
}
