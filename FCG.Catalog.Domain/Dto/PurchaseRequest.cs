namespace FCG.Catalog.Domain.Dto;

public sealed class PurchaseRequest
{
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
}
