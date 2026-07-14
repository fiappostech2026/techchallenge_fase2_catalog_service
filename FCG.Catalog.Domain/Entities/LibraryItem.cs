namespace FCG.Catalog.Domain.Entities;

public sealed class LibraryItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public DateTime AcquiredAt { get; init; } = DateTime.UtcNow;
}
