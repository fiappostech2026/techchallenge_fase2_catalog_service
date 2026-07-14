using FCG.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Catalog.Infra.Config;

public sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(game => game.Id);

        builder.Property(game => game.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(game => game.Description)
            .HasMaxLength(2000);

        builder.Property(game => game.Genre)
            .HasMaxLength(100);

        builder.Property(game => game.Price)
            .HasPrecision(10, 2);
    }
}
