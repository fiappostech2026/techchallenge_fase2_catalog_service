using System.Security.Claims;
using FCG.Catalog.Domain.Dto;
using FCG.Catalog.Domain.Interfaces.IService;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Catalog.Api.Controllers;

[ApiController]
[Route("api/v1/games/{gameId:guid}/reviews")]
public sealed class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly IValidator<ReviewRequest> _validator;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(
        IReviewService reviewService,
        IValidator<ReviewRequest> validator,
        ILogger<ReviewsController> logger)
    {
        _reviewService = reviewService;
        _validator = validator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ObterPorJogo(Guid gameId)
    {
        try
        {
            var reviews = await _reviewService.GetByGameIdAsync(gameId);
            return Ok(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter avaliacoes do jogo {GameId}.", gameId);
            throw;
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Criar(Guid gameId, [FromBody] ReviewRequest request)
    {
        try
        {
            var validation = await _validator.ValidateAsync(request);
            if (!validation.IsValid)
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

            var review = await _reviewService.CreateAsync(gameId, userId, userName, request);
            return CreatedAtAction(nameof(ObterPorJogo), new { gameId }, review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar avaliacao do jogo {GameId}.", gameId);
            throw;
        }
    }
}
