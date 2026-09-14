using FCG.Catalog.Domain.Interfaces;
using FCG.Catalog.Infra.Cache;

namespace FCG.Catalog.Api.Extensions;

public static class RedisExtension
{
    public static WebApplicationBuilder AddRedisCache(this WebApplicationBuilder builder)
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration["Redis:ConnectionString"];
            options.InstanceName = "catalog:";
        });

        builder.Services.AddScoped<ICacheService, RedisCacheService>();

        return builder;
    }
}
