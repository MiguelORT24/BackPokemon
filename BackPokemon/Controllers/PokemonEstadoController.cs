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
[Route("api/PokemonEstado")]
public sealed class PokemonEstadoController(ApplicationDbContext context) : ControllerBase
{
    [HttpPut("{idPokemonUser:int}")]
    public async Task<IActionResult> Update(int idPokemonUser, ActualizarPokemonEstadoDto request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Estado)) return BadRequest("El estado indicado no es válido.");
        var estado = await context.PokemonEstados.Include(value => value.PokemonUser)
            .FirstOrDefaultAsync(value => value.IdPokemonUser == idPokemonUser, cancellationToken);
        if (estado is null) return NotFound();
        if (estado.PokemonUser.IdUsuario != User.FindFirstValue(ClaimTypes.NameIdentifier)) return Forbid();
        if (request.VidaActual < 0 || request.VidaActual > estado.VidaTotal)
            return BadRequest("VidaActual debe estar entre 0 y VidaTotal.");
        estado.VidaActual = request.VidaActual;
        estado.Estado = request.Estado;
        estado.TurnosEstado = 0;
        estado.DuracionEstado = 0;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}