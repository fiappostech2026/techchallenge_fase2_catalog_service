using System.Security.Claims;
using FCG.Catalog.Domain.Interfaces.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Catalog.Api.Controllers;

[ApiController]
[Route("api/v1/users/{userId:guid}/library")]
[Authorize]
public sealed class LibraryController : ControllerBase
{
    private readonly ILibraryRepository _libraryRepository;
    private readonly ILogger<LibraryController> _logger;

    public LibraryController(ILibraryRepository libraryRepository, ILogger<LibraryController> logger)
    {
        _libraryRepository = libraryRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ObterBiblioteca(Guid userId)
    {
        try
        {
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(callerId, out var parsedCallerId) || parsedCallerId != userId)
                return Forbid();

            var itens = await _libraryRepository.ObterPorUsuarioAsync(userId);
            return Ok(itens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter a biblioteca do usuário {UserId}.", userId);
            throw;
        }
    }
}
