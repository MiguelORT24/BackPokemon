using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BackPokemon.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PokedexController : ControllerBase
{
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private readonly ApplicationDbContext _context;

    public PokedexController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PokedexDto>>> GetAll(CancellationToken cancellationToken)
    {
        var entries = await _context.Pokedex
            .AsNoTracking()
            .Where(entry => entry.UserId == CurrentUserId)
            .Select(entry => ToDto(entry))
            .ToListAsync(cancellationToken);

        return Ok(entries);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PokedexDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var entry = await _context.Pokedex
            .AsNoTracking()
            .FirstOrDefaultAsync(value => value.Id == id && value.UserId == CurrentUserId, cancellationToken);

        return entry is null ? NotFound() : Ok(ToDto(entry));
    }

    [HttpGet("/api/me/pokedex")]
    public Task<ActionResult<IEnumerable<PokedexDto>>> GetMine(CancellationToken cancellationToken) =>
        GetAll(cancellationToken);

    private static PokedexDto ToDto(Pokedex entry) => new()
    {
        Id = entry.Id,
        UserId = entry.UserId,
        PokemonId = entry.PokemonId,
        Fecha = entry.Fecha
    };
}
