using FCG.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCG.Catalog.Infra.Context;

public class CatalogContext : DbContext
{
    public CatalogContext(DbContextOptions<CatalogContext> options) : base(options) { }

    public DbSet<Game> Games => Set<Game>();
    public DbSet<LibraryItem> Libraries => Set<LibraryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
