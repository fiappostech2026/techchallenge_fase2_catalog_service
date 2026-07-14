using FCG.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Catalog.Infra.Config;

public sealed class LibraryItemConfiguration : IEntityTypeConfiguration<LibraryItem>
{
    public void Configure(EntityTypeBuilder<LibraryItem> builder)
    {
        builder.HasKey(item => item.Id);

        builder.HasIndex(item => new { item.UserId, item.GameId })
            .IsUnique();
    }
}
