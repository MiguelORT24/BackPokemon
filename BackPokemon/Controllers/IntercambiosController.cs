using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IntercambiosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public IntercambiosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<IntercambioDto>>> GetAll(CancellationToken cancellationToken)
    {
        var exchanges = await _context.Intercambios
            .AsNoTracking()
            .Select(intercambio => ToDto(intercambio))
            .ToListAsync(cancellationToken);

        return Ok(exchanges);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IntercambioDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var intercambio = await _context.Intercambios
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return intercambio is null ? NotFound() : Ok(ToDto(intercambio));
    }

    [HttpPost]
    public async Task<ActionResult<IntercambioDto>> Create(
        CreateIntercambioDto request,
        CancellationToken cancellationToken)
    {
        if (!await PokemonUsersExist(request.PokemonUserR, request.PokemonUserD, cancellationToken))
        {
            return NotFound("Uno o ambos PokemonUser no existen.");
        }

        var intercambio = new Intercambio
        {
            PokemonUserR = request.PokemonUserR,
            PokemonUserD = request.PokemonUserD,
            Fecha = request.Fecha,
            Estado = request.Estado,
            UserIdR = request.UserIdR,
            UserIdD = request.UserIdD
        };

        _context.Intercambios.Add(intercambio);
        await _context.SaveChangesAsync(cancellationToken);

        var response = ToDto(intercambio);
        return CreatedAtAction(nameof(GetById), new { id = intercambio.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateIntercambioDto request,
        CancellationToken cancellationToken)
    {
        var intercambio = await _context.Intercambios
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (intercambio is null)
        {
            return NotFound();
        }

        if (!await PokemonUsersExist(request.PokemonUserR, request.PokemonUserD, cancellationToken))
        {
            return NotFound("Uno o ambos PokemonUser no existen.");
        }

        intercambio.PokemonUserR = request.PokemonUserR;
        intercambio.PokemonUserD = request.PokemonUserD;
        intercambio.Fecha = request.Fecha;
        intercambio.Estado = request.Estado;
        intercambio.UserIdR = request.UserIdR;
        intercambio.UserIdD = request.UserIdD;
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var intercambio = await _context.Intercambios
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (intercambio is null)
        {
            return NotFound();
        }

        _context.Intercambios.Remove(intercambio);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<bool> PokemonUsersExist(
        int remitenteId,
        int destinatarioId,
        CancellationToken cancellationToken)
    {
        return await _context.PokemonUsers
            .CountAsync(
                pokemonUser => pokemonUser.Id == remitenteId || pokemonUser.Id == destinatarioId,
                cancellationToken) == 2;
    }

    private static IntercambioDto ToDto(Intercambio intercambio) => new()
    {
        Id = intercambio.Id,
        PokemonUserR = intercambio.PokemonUserR,
        PokemonUserD = intercambio.PokemonUserD,
        Fecha = intercambio.Fecha,
        Estado = intercambio.Estado,
        UserIdR = intercambio.UserIdR,
        UserIdD = intercambio.UserIdD
    };
}
