using System.Security.Claims;
using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PokedexController(ApplicationDbContext context, IConfiguration configuration) : ControllerBase
{
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PokedexDto>>> GetAll(CancellationToken cancellationToken)
    {
        if (CurrentUserId is null) return Unauthorized();
        var entries = await context.Pokedex.AsNoTracking().Where(entry => entry.UserId == CurrentUserId)
            .Select(entry => ToDto(entry)).ToListAsync(cancellationToken);
        return Ok(entries);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PokedexDto>> GetById(int id, CancellationToken cancellationToken)
    {
        if (CurrentUserId is null) return Unauthorized();
        var entry = await context.Pokedex.AsNoTracking()
            .FirstOrDefaultAsync(value => value.Id == id && value.UserId == CurrentUserId, cancellationToken);
        return entry is null ? NotFound() : Ok(ToDto(entry));
    }

    [HttpGet("/api/me/pokedex")]
    public Task<ActionResult<IEnumerable<PokedexDto>>> GetMine(CancellationToken cancellationToken) => GetAll(cancellationToken);

    [HttpGet("usuario/{userId}")]
    public async Task<IActionResult> GetByUsuario(string userId, CancellationToken cancellationToken)
    {
        if (!await context.Users.AnyAsync(value => value.Id == userId, cancellationToken)) return NotFound();
        var species = await context.Pokedex.AsNoTracking().Where(value => value.UserId == userId)
            .OrderBy(value => value.PokemonId)
            .Select(value => new { idPokemon = value.PokemonId, fecha = value.Fecha })
            .ToListAsync(cancellationToken);
        return Ok(new
        {
            registrados = species.Count,
            total = Math.Max(1, configuration.GetValue("Game:TotalSpecies", 151)),
            especies = species
        });
    }

    private static PokedexDto ToDto(Pokedex entry) => new()
    {
        Id = entry.Id,
        UserId = entry.UserId,
        PokemonId = entry.PokemonId,
        Fecha = entry.Fecha
    };
}