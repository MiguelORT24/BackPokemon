using System.Data;
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
[Route("api/encuentros")]
public sealed class EncuentrosController(ApplicationDbContext context, PokeApiService pokeApi) : ControllerBase
{
    private const double ZoneSizeDegrees = 0.01;

    [Authorize]
    [HttpGet("cercanos")]
    public async Task<IActionResult> GetNearby([FromQuery] double latitud, [FromQuery] double longitud,
        [FromQuery] double radioMetros = 1000, CancellationToken cancellationToken = default)
    {
        if (latitud is < -90 or > 90 || longitud is < -180 or > 180 || radioMetros is <= 0 or > 5000)
            return BadRequest("Coordenadas o radio fuera de rango.");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var now = DateTime.UtcNow;
        // Expired spawns and their per-user states are disposable. Capture history is retained
        // because its FK to Encuentro uses SET NULL.
        await context.Encuentros.Where(value => value.FechaExpiracion <= now)
            .ExecuteDeleteAsync(cancellationToken);

        var spawn = await EnsureZoneSpawn(latitud, longitud, cancellationToken);
        if (spawn is null) return StatusCode(StatusCodes.Status502BadGateway,
            "No se pudo generar un encuentro desde PokéAPI.");

        var latitudeDelta = radioMetros / 111_320d;
        var longitudeDelta = Math.Min(180d,
            radioMetros / (111_320d * Math.Max(0.01d, Math.Cos(latitud * Math.PI / 180d))));
        var minLatitude = Math.Max(-90d, latitud - latitudeDelta);
        var maxLatitude = Math.Min(90d, latitud + latitudeDelta);
        var minLongitude = longitud - longitudeDelta;
        var maxLongitude = longitud + longitudeDelta;
        var query = context.Encuentros.AsNoTracking().Where(value => value.FechaExpiracion > now
            && !value.ZonaKey.StartsWith("legacy-")
            && value.Latitud >= minLatitude && value.Latitud <= maxLatitude);
        query = minLongitude < -180d
            ? query.Where(value => value.Longitud >= minLongitude + 360d || value.Longitud <= maxLongitude)
            : maxLongitude > 180d
                ? query.Where(value => value.Longitud >= minLongitude || value.Longitud <= maxLongitude - 360d)
                : query.Where(value => value.Longitud >= minLongitude && value.Longitud <= maxLongitude);

        var encounters = await query.ToListAsync(cancellationToken);
        var encounterIds = encounters.Select(value => value.Id).ToArray();
        var states = await context.EncuentrosUsuario.AsNoTracking()
            .Where(value => value.UsuarioId == userId && encounterIds.Contains(value.EncuentroId))
            .ToDictionaryAsync(value => value.EncuentroId, cancellationToken);

        var nearby = encounters.Where(value => DistanceMeters(latitud, longitud, value.Latitud, value.Longitud) <= radioMetros
                && (!states.TryGetValue(value.Id, out var state) || !state.Capturado))
            .Select(value =>
            {
                states.TryGetValue(value.Id, out var state);
                return new EncuentroDto(value.Id, value.PokemonId, value.PokemonNombre,
                    value.Latitud, value.Longitud, value.FechaExpiracion, value.Dormido,
                    state?.ComiendoHasta > now, state?.JugadorDetectado ?? false,
                    state?.TiroPorLaEspaldaDisponible ?? false);
            });
        return Ok(nearby);
    }

    // Retained as an administrative tool for staging events and testing. Normal play
    // generates one shared random spawn per geographic cell on demand.
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

        var now = DateTime.UtcNow;
        var encounter = new Encuentro
        {
            ZonaKey = $"manual-{Guid.NewGuid():N}", PokemonId = pokemon.Id,
            PokemonNombre = pokemon.Nombre, Latitud = request.Latitud, Longitud = request.Longitud,
            FechaExpiracion = now.AddHours(1), Dormido = request.Dormido
        };
        context.Encuentros.Add(encounter);
        await context.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetNearby), new { latitud = request.Latitud, longitud = request.Longitud },
            new EncuentroDto(encounter.Id, encounter.PokemonId, encounter.PokemonNombre,
                encounter.Latitud, encounter.Longitud, encounter.FechaExpiracion,
                encounter.Dormido, false, false, false));
    }

    [Authorize(Roles = "GameAdmin")]
    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> SetState(int id, ActualizarEstadoEncuentroDto request,
        CancellationToken cancellationToken)
    {
        var encounter = await context.Encuentros.FirstOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (encounter is null) return NotFound();
        if (encounter.FechaExpiracion <= DateTime.UtcNow) return Conflict("El encuentro ya expiró.");
        encounter.Dormido = request.Dormido;

        if (!string.IsNullOrWhiteSpace(request.UsuarioId))
        {
            if (!await context.Users.AnyAsync(user => user.Id == request.UsuarioId, cancellationToken))
                return NotFound("El usuario no existe.");
            var state = await context.EncuentrosUsuario.FirstOrDefaultAsync(value =>
                value.EncuentroId == id && value.UsuarioId == request.UsuarioId, cancellationToken);
            if (state is null)
            {
                state = new EncuentroUsuario { EncuentroId = id, UsuarioId = request.UsuarioId };
                context.EncuentrosUsuario.Add(state);
            }
            state.JugadorDetectado = request.JugadorDetectado;
            state.TiroPorLaEspaldaDisponible = request.TiroPorLaEspaldaDisponible;
            state.CalidadTiroDisponible = request.CalidadTiroDisponible;
        }

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<Encuentro?> EnsureZoneSpawn(double latitude, double longitude,
        CancellationToken cancellationToken)
    {
        var (zoneKey, centerLatitude, centerLongitude) = ZoneFor(latitude, longitude);
        var now = DateTime.UtcNow;
        var existing = await context.Encuentros.AsNoTracking()
            .FirstOrDefaultAsync(value => value.ZonaKey == zoneKey && value.FechaExpiracion > now,
                cancellationToken);
        if (existing is not null) return existing;

        PokeApiPokemon pokemon;
        try
        {
            // Base species IDs are continuous in the PokeAPI Pokémon endpoint.
            pokemon = await pokeApi.GetPokemon(Random.Shared.Next(1, 1026), cancellationToken);
        }
        catch (PokeApiPokemonNotFoundException) { return null; }
        catch (PokeApiUnavailableException) { return null; }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { return null; }

        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);
            existing = await context.Encuentros.FirstOrDefaultAsync(value => value.ZonaKey == zoneKey,
                cancellationToken);
            if (existing is not null && existing.FechaExpiracion > DateTime.UtcNow)
            {
                await transaction.CommitAsync(cancellationToken);
                return existing;
            }

            if (existing is not null)
                await context.Encuentros.Where(value => value.Id == existing.Id)
                    .ExecuteDeleteAsync(cancellationToken);

            var encounter = new Encuentro
            {
                ZonaKey = zoneKey, PokemonId = pokemon.Id, PokemonNombre = pokemon.Nombre,
                Latitud = centerLatitude, Longitud = centerLongitude,
                FechaExpiracion = DateTime.UtcNow.AddHours(1), Dormido = false
            };
            context.Encuentros.Add(encounter);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return encounter;
        }
        catch (DbUpdateException)
        {
            context.ChangeTracker.Clear();
            return await context.Encuentros.AsNoTracking().FirstOrDefaultAsync(
                value => value.ZonaKey == zoneKey && value.FechaExpiracion > DateTime.UtcNow,
                cancellationToken);
        }
    }

    private static (string Key, double Latitude, double Longitude) ZoneFor(double latitude, double longitude)
    {
        var y = (int)Math.Floor(latitude / ZoneSizeDegrees);
        var x = (int)Math.Floor(longitude / ZoneSizeDegrees);
        var centerLatitude = Math.Clamp((y + 0.5d) * ZoneSizeDegrees, -89.995d, 89.995d);
        var centerLongitude = Math.Clamp((x + 0.5d) * ZoneSizeDegrees, -179.995d, 179.995d);
        return ($"{y}:{x}", centerLatitude, centerLongitude);
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
