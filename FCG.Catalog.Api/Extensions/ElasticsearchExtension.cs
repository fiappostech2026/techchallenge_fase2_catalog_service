using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using FCG.Catalog.Domain.Interfaces;
using FCG.Catalog.Domain.Interfaces.IService;
using FCG.Catalog.Infra.Search;

namespace FCG.Catalog.Api.Extensions;

public static class ElasticsearchExtension
{
    public static WebApplicationBuilder AddElasticsearch(this WebApplicationBuilder builder)
    {
        var settings = builder.Configuration.GetSection("Elasticsearch").Get<ElasticsearchSettings>() ?? new ElasticsearchSettings();

        var clientSettings = new ElasticsearchClientSettings(new Uri(settings.Url))
            .DefaultIndex(settings.IndexName)
            .RequestTimeout(TimeSpan.FromSeconds(10));

        // Elastic Cloud exige API Key; o container local (docker-compose) roda sem segurança.
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            clientSettings = clientSettings.Authentication(new ApiKey(settings.ApiKey));

        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(new ElasticsearchClient(clientSettings));
        builder.Services.AddScoped<ISearchService, ElasticsearchService>();

        return builder;
    }

    // Na subida, cria o índice (se não existir) e reindexa todos os jogos do SQL Server.
    // Assim o índice nunca fica vazio depois de um deploy novo ou de um índice recriado.
    // Se o Elasticsearch estiver fora do ar, a API sobe mesmo assim — só a busca fica indisponível.
    public static async Task<WebApplication> InitializeSearchIndexAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();

        try
        {
            var total = await gameService.ReindexAllAsync();
            app.Logger.LogInformation("Índice de busca sincronizado com {Total} jogos.", total);
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Não foi possível sincronizar o índice de busca na inicialização.");
        }

        return app;
    }
}
