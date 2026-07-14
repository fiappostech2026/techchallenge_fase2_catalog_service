using System.Security.Claims;
using FCG.Catalog.Domain.Dto;
using FCG.Catalog.Domain.Interfaces.IRepository;
using FCG.Catalog.Domain.Interfaces.IService;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Catalog.Api.Controllers;

[ApiController]
[Route("api/v1/purchases")]
[Authorize]
public sealed class PurchasesController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;
    private readonly ILibraryRepository _libraryRepository;
    private readonly IValidator<PurchaseRequest> _validator;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<PurchasesController> _logger;

    public PurchasesController(
        IPurchaseService purchaseService,
        ILibraryRepository libraryRepository,
        IValidator<PurchaseRequest> validator,
        IPublishEndpoint publishEndpoint,
        ILogger<PurchasesController> logger)
    {
        _purchaseService = purchaseService;
        _libraryRepository = libraryRepository;
        _validator = validator;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Comprar([FromBody] PurchaseRequest request)
    {
        try
        {
            var validation = await _validator.ValidateAsync(request);
            if (!validation.IsValid)
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(callerId, out var parsedCallerId) || parsedCallerId != request.UserId)
                return StatusCode(403, "Você não pode realizar compras para outro usuário.");

            if (await _libraryRepository.ExistsAsync(request.UserId, request.GameId))
                return Conflict("Você já possui esse jogo em sua biblioteca.");

            var orderPlaced = await _purchaseService.CreateOrderAsync(request);
            if (orderPlaced is null)
                return NotFound("Jogo não encontrado.");

            await _publishEndpoint.Publish(orderPlaced);

            return Accepted(new { orderPlaced.OrderId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar a compra.");
            throw;
        }
    }
}
