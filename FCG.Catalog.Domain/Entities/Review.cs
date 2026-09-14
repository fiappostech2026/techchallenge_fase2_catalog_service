namespace FCG.Catalog.Domain.Entities;

public sealed class Review
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid GameId { get; init; }
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string Comment { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
