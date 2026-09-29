using System.Security.Claims;
using BackPokemon.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BackPokemon.Data;  

[Authorize]
[ApiController]
[Route("api/me")]
public class UsuarioController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UsuarioController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("pokemon")]
    public async Task<IActionResult> GetMyPokemon(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return Unauthorized();
        }

        var pokemon = await _context.PokemonUsers
            .Where(p => p.IdUsuario == userId)
            .Select(p => new PokemonUserDto
            {
                Id = p.Id,
                IdPokemon = p.IdPokemon,
                IdUsuario = p.IdUsuario
            })
            .ToListAsync(cancellationToken);

        return Ok(pokemon);
    }
}