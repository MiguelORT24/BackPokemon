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
[Route("api/PokemonUser")]
public class PokemonUsersController(ApplicationDbContext context) : ControllerBase
{
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PokemonUserDto>>> GetAll(CancellationToken cancellationToken)
    {
        if (CurrentUserId is null) return Unauthorized();
        return Ok(await context.PokemonUsers.AsNoTracking().Where(value => value.IdUsuario == CurrentUserId)
            .Select(value => ToDto(value)).ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PokemonUserDto>> GetById(int id, CancellationToken cancellationToken)
    {
        if (CurrentUserId is null) return Unauthorized();
        var pokemon = await context.PokemonUsers.AsNoTracking()
            .FirstOrDefaultAsync(value => value.Id == id && value.IdUsuario == CurrentUserId, cancellationToken);
        return pokemon is null ? NotFound() : Ok(ToDto(pokemon));
    }

    // Public tracker view: safe game data only; no email or Identity fields are serialized.
    [HttpGet("usuario/{userId}")]
    public async Task<ActionResult<IEnumerable<PokemonUserConEstadoDto>>> GetByUsuario(string userId,
        CancellationToken cancellationToken)
    {
        var exists = await context.Users.AnyAsync(value => value.Id == userId, cancellationToken);
        if (!exists) return NotFound();
        var pokemon = await context.PokemonUsers.AsNoTracking().Where(value => value.IdUsuario == userId)
            .Select(value => new PokemonUserConEstadoDto
            {
                Id = value.Id,
                IdPokemon = value.IdPokemon,
                IdUsuario = value.IdUsuario,
                Nombre = value.Nombre,
                FechaObtencion = value.FechaObtencion,
                Origen = value.Origen,
                PokemonEstado = value.EstadoDetalle == null ? null : new PokemonEstadoDto
                {
                    VidaTotal = value.EstadoDetalle.VidaTotal,
                    VidaActual = value.EstadoDetalle.VidaActual,
                    Estado = value.EstadoDetalle.Estado,
                    EstadoNombre = value.EstadoDetalle.Estado.ToString(),
                    AtaqueBase = value.EstadoDetalle.AtaqueBase,
                    DefensaBase = value.EstadoDetalle.DefensaBase,
                    VelocidadBase = value.EstadoDetalle.VelocidadBase,
                    TipoPrimario = value.EstadoDetalle.TipoPrimario,
                    TipoSecundario = value.EstadoDetalle.TipoSecundario,
                    UrlFrontal = value.EstadoDetalle.UrlFrontal,
                    UrlTrasera = value.EstadoDetalle.UrlTrasera
                },
                Crianza = value.Crianza == null ? null : new PokemonCrianzaResumenDto
                {
                    Hambre = value.Crianza.Hambre,
                    Energia = value.Crianza.Energia,
                    Higiene = value.Crianza.Higiene,
                    Vivo = value.Crianza.Vivo,
                    FechaAdopcion = value.Crianza.FechaAdopcion,
                    FechaMuerte = value.Crianza.FechaMuerte
                }
            }).ToListAsync(cancellationToken);
        return Ok(pokemon);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdatePokemonUserDto request, CancellationToken cancellationToken)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (request.IdUsuario != CurrentUserId) return Forbid();
        var pokemon = await context.PokemonUsers.FirstOrDefaultAsync(value => value.Id == id
            && value.IdUsuario == CurrentUserId, cancellationToken);
        if (pokemon is null) return NotFound();
        if (pokemon.IdPokemon != request.IdPokemon)
            return Conflict("No se puede cambiar la especie de un Pokémon existente.");
        pokemon.Nombre = request.Nombre;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (CurrentUserId is null) return Unauthorized();
        var pokemon = await context.PokemonUsers.FirstOrDefaultAsync(value => value.Id == id
            && value.IdUsuario == CurrentUserId, cancellationToken);
        if (pokemon is null) return NotFound();
        if (await context.Intercambios.AnyAsync(value => value.PokemonUserR == id || value.PokemonUserD == id,
            cancellationToken)) return Conflict("El Pokémon tiene historial de intercambios y no se puede eliminar.");
        context.PokemonUsers.Remove(pokemon);
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static PokemonUserDto ToDto(PokemonUser pokemon) => new()
    {
        Id = pokemon.Id,
        IdPokemon = pokemon.IdPokemon,
        IdUsuario = pokemon.IdUsuario,
        Nombre = pokemon.Nombre,
        FechaObtencion = pokemon.FechaObtencion,
        Origen = pokemon.Origen
    };
}