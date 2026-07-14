using FCG.Catalog.Domain.Dto;
using FCG.Catalog.Domain.Interfaces.IService;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Catalog.Api.Controllers;

[ApiController]
[Route("api/v1/games")]
public sealed class GamesController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly IValidator<GameRequest> _validator;
    private readonly ILogger<GamesController> _logger;

    public GamesController(
        IGameService gameService,
        IValidator<GameRequest> validator,
        ILogger<GamesController> logger)
    {
        _gameService = gameService;
        _validator = validator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ObterTodos()
    {
        try
        {
            var games = await _gameService.GetAllAsync();
            return Ok(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter todos os jogos.");
            throw;
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        try
        {
            var game = await _gameService.GetByIdAsync(id);
            return game is null ? NotFound() : Ok(game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter o jogo {Id}.", id);
            throw;
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Criar([FromBody] GameRequest request)
    {
        try
        {
            var validation = await _validator.ValidateAsync(request);
            if (!validation.IsValid)
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

            var game = await _gameService.CreateAsync(request);
            return CreatedAtAction(nameof(ObterPorId), new { id = game.Id }, game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar o jogo.");
            throw;
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] GameRequest request)
    {
        try
        {
            var validation = await _validator.ValidateAsync(request);
            if (!validation.IsValid)
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

            var game = await _gameService.UpdateAsync(id, request);
            return game is null ? NotFound() : Ok(game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar o jogo {Id}.", id);
            throw;
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Excluir(Guid id)
    {
        try
        {
            var excluido = await _gameService.DeleteAsync(id);
            return excluido ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir o jogo {Id}.", id);
            throw;
        }
    }
}
