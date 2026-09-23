using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PokedexController : ControllerBase
{
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
            .Select(entry => ToDto(entry))
            .ToListAsync(cancellationToken);

        return Ok(entries);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PokedexDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var entry = await _context.Pokedex
            .AsNoTracking()
            .FirstOrDefaultAsync(value => value.Id == id, cancellationToken);

        return entry is null ? NotFound() : Ok(ToDto(entry));
    }

    [HttpPost]
    public async Task<ActionResult<PokedexDto>> Create(
        CreatePokedexDto request,
        CancellationToken cancellationToken)
    {
        if (!await _context.Users.AnyAsync(user => user.Id == request.UserId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.UserId), "El usuario indicado no existe.");
            return ValidationProblem(ModelState);
        }

        var entry = new Pokedex
        {
            UserId = request.UserId,
            PokemonId = request.PokemonId,
            Fecha = DateTime.UtcNow
        };

        _context.Pokedex.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        var response = ToDto(entry);
        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        CreatePokedexDto request,
        CancellationToken cancellationToken)
    {
        var entry = await _context.Pokedex
            .FirstOrDefaultAsync(value => value.Id == id, cancellationToken);

        if (entry is null)
        {
            return NotFound();
        }

        if (!await _context.Users.AnyAsync(user => user.Id == request.UserId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.UserId), "El usuario indicado no existe.");
            return ValidationProblem(ModelState);
        }

        entry.UserId = request.UserId;
        entry.PokemonId = request.PokemonId;
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entry = await _context.Pokedex
            .FirstOrDefaultAsync(value => value.Id == id, cancellationToken);

        if (entry is null)
        {
            return NotFound();
        }

        _context.Pokedex.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static PokedexDto ToDto(Pokedex entry) => new()
    {
        Id = entry.Id,
        UserId = entry.UserId,
        PokemonId = entry.PokemonId,
        Fecha = entry.Fecha
    };
}
