namespace FCG.Catalog.Domain.Dto;

public sealed class ReviewRequest
{
    public int Rating { get; init; }
    public string Comment { get; init; } = string.Empty;
}
