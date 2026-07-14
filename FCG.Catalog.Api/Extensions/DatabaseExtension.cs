using FCG.Catalog.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace FCG.Catalog.Api.Extensions;

public static class DatabaseExtension
{
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<CatalogContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        return builder;
    }

    public static WebApplication MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        db.Database.Migrate();

        return app;
    }
}
