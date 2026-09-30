using System.Security.Claims;
using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using BackPokemon.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Controllers;

[ApiController]
[Authorize]
[Route("api/tamagotchi")]
public sealed class TamagotchiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly TamagotchiService _service;

    public TamagotchiController(
        ApplicationDbContext context,
        TamagotchiService service)
    {
        _context = context;
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TamagotchiDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var mascotas = await _context.PokemonCrianzas
            .Include(crianza => crianza.PokemonUser)
                .ThenInclude(pokemonUser => pokemonUser.EstadoDetalle)
            .Where(crianza => crianza.PokemonUser.IdUsuario == userId)
            .ToListAsync(cancellationToken);

        foreach (var mascota in mascotas)
        {
            _service.ActualizarEstado(mascota);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(mascotas.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TamagotchiDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var mascota = await BuscarMascota(id, userId, cancellationToken);

        if (mascota is null)
        {
            return NotFound();
        }

        _service.ActualizarEstado(mascota);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ToDto(mascota));
    }

    [HttpPost]
    public async Task<ActionResult<TamagotchiDto>> Adoptar(
        AdoptarTamagotchiDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var cantidad = await _context.PokemonCrianzas
            .CountAsync(
                crianza => crianza.PokemonUser.IdUsuario == userId
                    && crianza.Vivo,
                cancellationToken);

        if (cantidad >= _service.Limite)
        {
            return BadRequest($"El usuario no puede tener más de {_service.Limite} Tamagotchis vivos.");
        }

        var pokemonUser = await _context.PokemonUsers
            .Include(pokemon => pokemon.EstadoDetalle)
            .FirstOrDefaultAsync(
                pokemon => pokemon.Id == request.IdPokemonUser
                    && pokemon.IdUsuario == userId,
                cancellationToken);

        if (pokemonUser is null)
        {
            return NotFound("El Pokémon no existe o no pertenece al usuario.");
        }

        if (pokemonUser.EstadoDetalle is null)
        {
            return BadRequest("El Pokémon no tiene un estado registrado.");
        }

        var yaAdoptado = await _context.PokemonCrianzas
            .AnyAsync(
                crianza => crianza.IdPokemonUser == request.IdPokemonUser,
                cancellationToken);

        if (yaAdoptado)
        {
            return Conflict("Este Pokémon ya es un Tamagotchi.");
        }

        var ahora = DateTime.UtcNow;
        var crianza = new PokemonCrianza
        {
            IdPokemonUser = pokemonUser.Id,
            PokemonUser = pokemonUser,
            FechaAdopcion = ahora,
            UltimaActualizacion = ahora
        };

        _context.PokemonCrianzas.Add(crianza);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = crianza.Id },
            ToDto(crianza));
    }

    [HttpPost("{id:int}/actividad")]
    public async Task<ActionResult<TamagotchiDto>> Actividad(
        int id,
        ActividadTamagotchiDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var mascota = await BuscarMascota(id, userId, cancellationToken);

        if (mascota is null)
        {
            return NotFound();
        }

        _service.ActualizarEstado(mascota);

        if (!mascota.Vivo)
        {
            await _context.SaveChangesAsync(cancellationToken);
            return BadRequest(new
            {
                mensaje = "El Tamagotchi ha muerto porque su salud llegó a cero.",
                tamagotchi = ToDto(mascota)
            });
        }

        try
        {
            _service.AplicarActividad(mascota, request.Tipo);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { mensaje = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { mensaje = exception.Message });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ToDto(mascota));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Liberar(
        int id,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var mascota = await _context.PokemonCrianzas
            .Include(crianza => crianza.PokemonUser)
            .FirstOrDefaultAsync(
                crianza => crianza.Id == id
                    && crianza.PokemonUser.IdUsuario == userId,
                cancellationToken);

        if (mascota is null)
        {
            return NotFound();
        }

        _context.PokemonCrianzas.Remove(mascota);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<PokemonCrianza?> BuscarMascota(
        int id,
        string userId,
        CancellationToken cancellationToken)
    {
        return await _context.PokemonCrianzas
            .Include(crianza => crianza.PokemonUser)
                .ThenInclude(pokemonUser => pokemonUser.EstadoDetalle)
            .FirstOrDefaultAsync(
                crianza => crianza.Id == id
                    && crianza.PokemonUser.IdUsuario == userId,
                cancellationToken);
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static TamagotchiDto ToDto(PokemonCrianza crianza)
    {
        var estado = crianza.PokemonUser.EstadoDetalle;

        return new TamagotchiDto
        {
            Id = crianza.Id,
            IdPokemonUser = crianza.IdPokemonUser,
            IdPokemon = crianza.PokemonUser.IdPokemon,
            Hambre = crianza.Hambre,
            Energia = crianza.Energia,
            Higiene = crianza.Higiene,
            VidaActual = estado?.VidaActual ?? 0,
            VidaTotal = estado?.VidaTotal ?? 0,
            Estado = estado?.Estado.ToString() ?? EstadoPokemon.Sano.ToString(),
            Vivo = crianza.Vivo,
            FechaAdopcion = crianza.FechaAdopcion
        };
    }
}
