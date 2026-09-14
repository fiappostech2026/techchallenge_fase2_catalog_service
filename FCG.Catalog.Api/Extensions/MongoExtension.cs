using FCG.Catalog.Infra.Mongo;
using MongoDB.Driver;

namespace FCG.Catalog.Api.Extensions;

public static class MongoExtension
{
    public static WebApplicationBuilder AddMongo(this WebApplicationBuilder builder)
    {
        ReviewBsonMap.Register();

        var connectionString = builder.Configuration["Mongo:ConnectionString"];
        var databaseName = builder.Configuration["Mongo:Database"];

        builder.Services.AddSingleton<IMongoDatabase>(_ =>
        {
            var client = new MongoClient(connectionString);
            return client.GetDatabase(databaseName);
        });

        return builder;
    }
}
