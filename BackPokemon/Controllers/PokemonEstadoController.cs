using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Controllers;

[ApiController]
[Route("api/PokemonEstado")]
public sealed class PokemonEstadoController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PokemonEstadoController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPut("{idPokemonUser:int}")]
    public async Task<IActionResult> Update(
        int idPokemonUser,
        ActualizarPokemonEstadoDto request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Estado))
        {
            return BadRequest("El estado indicado no es válido.");
        }

        var estado = await _context.PokemonEstados
            .FirstOrDefaultAsync(
                item => item.IdPokemonUser == idPokemonUser,
                cancellationToken);

        if (estado is null)
        {
            return NotFound();
        }

        if (request.VidaActual < 0 || request.VidaActual > estado.VidaTotal)
        {
            return BadRequest("VidaActual debe estar entre 0 y VidaTotal.");
        }

        estado.VidaActual = request.VidaActual;
        estado.Estado = request.Estado;
        estado.TurnosEstado = 0;
        estado.DuracionEstado = 0;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
