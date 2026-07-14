using FCG.Catalog.Domain.Dto;

namespace FCG.Catalog.Domain.Interfaces.IService;

public interface IPurchaseService
{
    Task<OrderPlacedEvent?> CreateOrderAsync(PurchaseRequest request);
}
