namespace FCG.Catalog.Domain.Dto;

public sealed class GameRequest
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Genre { get; init; } = string.Empty;
}
