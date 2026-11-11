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
    [HttpPost]
    public async Task<ActionResult<PokemonEstadoDto>> Create(CrearPokemonEstadoDto request,
        CancellationToken cancellationToken)
    {
        if (request.VidaActual > request.VidaTotal)
            return BadRequest("VidaActual debe estar entre 0 y VidaTotal.");
        if (!Enum.IsDefined(request.Estado))
            return BadRequest("El estado indicado no es válido.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var pokemonUser = await context.PokemonUsers
            .FirstOrDefaultAsync(value => value.Id == request.IdPokemonUser, cancellationToken);
        if (pokemonUser is null) return NotFound("No existe el PokemonUser indicado.");
        if (pokemonUser.IdUsuario != userId) return Forbid();

        if (await context.PokemonEstados.AnyAsync(value => value.IdPokemonUser == request.IdPokemonUser,
            cancellationToken))
            return Conflict("El PokemonUser ya tiene un PokemonEstado.");

        var estado = new PokemonEstado
        {
            IdPokemonUser = request.IdPokemonUser,
            VidaTotal = request.VidaTotal,
            VidaActual = request.VidaActual,
            Estado = request.Estado,
            TurnosEstado = 0,
            DuracionEstado = 0,
            IdPokeball = request.IdPokeball,
            AtaqueBase = request.AtaqueBase,
            DefensaBase = request.DefensaBase,
            VelocidadBase = request.VelocidadBase,
            TipoPrimario = request.TipoPrimario,
            TipoSecundario = request.TipoSecundario,
            UrlFrontal = request.UrlFrontal,
            UrlTrasera = request.UrlTrasera
        };

        context.PokemonEstados.Add(estado);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Update), new { idPokemonUser = estado.IdPokemonUser }, ToDto(estado));
    }

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

    private static PokemonEstadoDto ToDto(PokemonEstado estado) => new()
    {
        IdPokemonUser = estado.IdPokemonUser,
        VidaTotal = estado.VidaTotal,
        VidaActual = estado.VidaActual,
        Estado = estado.Estado,
        EstadoNombre = estado.Estado.ToString(),
        IdPokeball = estado.IdPokeball,
        AtaqueBase = estado.AtaqueBase,
        DefensaBase = estado.DefensaBase,
        VelocidadBase = estado.VelocidadBase,
        TipoPrimario = estado.TipoPrimario,
        TipoSecundario = estado.TipoSecundario,
        UrlFrontal = estado.UrlFrontal,
        UrlTrasera = estado.UrlTrasera
    };
}
