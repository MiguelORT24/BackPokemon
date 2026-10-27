using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using BackPokemon.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Controllers;

[ApiController]
[Route("api/encuentros")]
public sealed class EncuentrosController(ApplicationDbContext context, PokeApiService pokeApi) : ControllerBase
{
    [Authorize]
    [HttpGet("cercanos")]
    public async Task<IActionResult> GetNearby([FromQuery] double latitud, [FromQuery] double longitud,
        [FromQuery] double radioMetros = 1000, CancellationToken cancellationToken = default)
    {
        if (latitud is < -90 or > 90 || longitud is < -180 or > 180 || radioMetros is <= 0 or > 5000)
            return BadRequest("Coordenadas o radio fuera de rango.");
        var now = DateTime.UtcNow;
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();
        var encounters = await context.Encuentros.AsNoTracking()
            .Where(value => value.UsuarioId == userId && !value.Capturado && value.FechaExpiracion > now)
            .ToListAsync(cancellationToken);
        return Ok(encounters.Where(value => DistanceMeters(latitud, longitud, value.Latitud, value.Longitud) <= radioMetros)
            .Select(value => new EncuentroDto(value.Id, value.PokemonId, value.PokemonNombre,
                value.Latitud, value.Longitud, value.FechaExpiracion, value.Dormido,
                value.ComiendoHasta > now, value.JugadorDetectado, value.TiroPorLaEspaldaDisponible)));
    }

    [Authorize(Roles = "GameAdmin")]
    [HttpPost]
    public async Task<IActionResult> Spawn(CrearEncuentroDto request, CancellationToken cancellationToken)
    {
        PokeApiPokemon pokemon;
        try { pokemon = await pokeApi.GetPokemon(request.PokemonId, cancellationToken); }
        catch (PokeApiPokemonNotFoundException) { return NotFound("El Pokémon no existe en PokéAPI."); }
        catch (PokeApiUnavailableException) { return StatusCode(502, "PokéAPI no está disponible."); }
        catch (HttpRequestException) { return StatusCode(502, "No se pudo conectar con PokéAPI."); }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return StatusCode(504, "La consulta a PokéAPI agotó el tiempo de espera."); }
        if (!await context.Users.AnyAsync(user => user.Id == request.UsuarioId, cancellationToken))
            return NotFound("El usuario no existe.");
        var encounter = new Encuentro
        {
            PokemonId = pokemon.Id, UsuarioId = request.UsuarioId, PokemonNombre = pokemon.Nombre, Latitud = request.Latitud,
            Longitud = request.Longitud, FechaExpiracion = DateTime.UtcNow.AddMinutes(request.DuracionMinutos),
            Dormido = request.Dormido, JugadorDetectado = false, TiroPorLaEspaldaDisponible = false
        };
        context.Encuentros.Add(encounter);
        await context.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetNearby), new { latitud = request.Latitud, longitud = request.Longitud },
            new EncuentroDto(encounter.Id, encounter.PokemonId, encounter.PokemonNombre, encounter.Latitud,
                encounter.Longitud, encounter.FechaExpiracion, encounter.Dormido, false,
                encounter.JugadorDetectado, encounter.TiroPorLaEspaldaDisponible));
    }

    [Authorize(Roles = "GameAdmin")]
    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> SetState(int id, ActualizarEstadoEncuentroDto request,
        CancellationToken cancellationToken)
    {
        var encounter = await context.Encuentros.FirstOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (encounter is null) return NotFound();
        if (encounter.Capturado || encounter.FechaExpiracion <= DateTime.UtcNow)
            return Conflict("El encuentro ya terminó o expiró.");
        encounter.Dormido = request.Dormido;
        encounter.JugadorDetectado = request.JugadorDetectado;
        encounter.TiroPorLaEspaldaDisponible = request.TiroPorLaEspaldaDisponible;
        encounter.CalidadTiroDisponible = request.CalidadTiroDisponible;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    internal static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6_371_000;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Pow(Math.Sin(dLon / 2), 2);
        return earthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}
