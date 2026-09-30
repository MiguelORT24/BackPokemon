using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BatallasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BatallasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BatallaDto>>> GetAll(CancellationToken cancellationToken)
    {
        var battles = await _context.Batallas
            .AsNoTracking()
            .Select(batalla => ToDto(batalla))
            .ToListAsync(cancellationToken);

        return Ok(battles);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BatallaDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var batalla = await _context.Batallas
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return batalla is null ? NotFound() : Ok(ToDto(batalla));
    }

    [HttpGet("usuario/{userId}")]
    public async Task<ActionResult<IEnumerable<BatallaDto>>> GetByUsuario(
        string userId,
        CancellationToken cancellationToken)
    {
        var battles = await _context.Batallas
            .AsNoTracking()
            .Where(batalla => batalla.PokemonUserRetador.IdUsuario == userId)
            .Select(batalla => ToDto(batalla))
            .ToListAsync(cancellationToken);

        return Ok(battles);
    }

    [HttpPost]
    public async Task<ActionResult<BatallaDto>> Create(
        CreateBatallaDto request,
        CancellationToken cancellationToken)
    {
        if (!await PokemonUsersExist(request, cancellationToken))
        {
            return NotFound("Uno o más PokemonUser no existen.");
        }

        var batalla = new Batalla
        {
            PokemonUserR = request.PokemonUserR,
            IdPokemonRival = request.IdPokemonRival,
            Fecha = request.Fecha,
            PokemonUserGanador = request.PokemonUserGanador
        };

        _context.Batallas.Add(batalla);
        await _context.SaveChangesAsync(cancellationToken);

        var response = ToDto(batalla);
        return CreatedAtAction(nameof(GetById), new { id = batalla.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateBatallaDto request,
        CancellationToken cancellationToken)
    {
        var batalla = await _context.Batallas
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (batalla is null)
        {
            return NotFound();
        }

        if (!await PokemonUsersExist(request, cancellationToken))
        {
            return NotFound("Uno o más PokemonUser no existen.");
        }

        batalla.PokemonUserR = request.PokemonUserR;
        batalla.IdPokemonRival = request.IdPokemonRival;
        batalla.Fecha = request.Fecha;
        batalla.PokemonUserGanador = request.PokemonUserGanador;
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var batalla = await _context.Batallas
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (batalla is null)
        {
            return NotFound();
        }

        _context.Batallas.Remove(batalla);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<bool> PokemonUsersExist(
        CreateBatallaDto request,
        CancellationToken cancellationToken)
    {
        var ids = new[] { request.PokemonUserR, request.PokemonUserGanador };
        var requiredCount = ids.Distinct().Count();

        var existingCount = await _context.PokemonUsers
            .Where(pokemonUser => ids.Contains(pokemonUser.Id))
            .Select(pokemonUser => pokemonUser.Id)
            .Distinct()
            .CountAsync(cancellationToken);

        return existingCount == requiredCount;
    }

    private async Task<bool> PokemonUsersExist(
        UpdateBatallaDto request,
        CancellationToken cancellationToken)
    {
        var ids = new[] { request.PokemonUserR, request.PokemonUserGanador };
        var requiredCount = ids.Distinct().Count();

        var existingCount = await _context.PokemonUsers
            .Where(pokemonUser => ids.Contains(pokemonUser.Id))
            .Select(pokemonUser => pokemonUser.Id)
            .Distinct()
            .CountAsync(cancellationToken);

        return existingCount == requiredCount;
    }

    private static BatallaDto ToDto(Batalla batalla) => new()
    {
        Id = batalla.Id,
        PokemonUserR = batalla.PokemonUserR,
        IdPokemonRival = batalla.IdPokemonRival,
        Fecha = batalla.Fecha,
        PokemonUserGanador = batalla.PokemonUserGanador
    };
}
