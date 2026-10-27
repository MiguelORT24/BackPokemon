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

[Authorize]
[ApiController]
[Route("api/capturas")]
public sealed class CapturasController(ApplicationDbContext context, PokeApiService pokeApi) : ControllerBase
{
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpPost]
    public async Task<IActionResult> Capturar(IntentarCapturaDto request, CancellationToken cancellationToken)
    {
        var userId = UserId;
        if (userId is null) return Unauthorized();

        var encounter = await context.Encuentros.AsNoTracking()
            .FirstOrDefaultAsync(value => value.Id == request.EncuentroId
                && (value.UsuarioId == null || value.UsuarioId == userId), cancellationToken);
        if (encounter is null) return NotFound("El encuentro no existe.");
        if (encounter.FechaExpiracion <= DateTime.UtcNow)
            return Conflict("El encuentro ya terminó o expiró.");

        PokeApiPokemon pokemon;
        try
        {
            pokemon = await pokeApi.GetPokemon(encounter.PokemonId, cancellationToken);
        }
        catch (PokeApiPokemonNotFoundException)
        {
            return NotFound("El Pokémon del encuentro no existe en PokéAPI.");
        }
        catch (PokeApiUnavailableException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "PokéAPI no pudo completar la consulta.");
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "No se pudo conectar con PokéAPI.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, "La consulta a PokéAPI agotó el tiempo de espera.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var currentEncounter = await context.Encuentros
            .FirstOrDefaultAsync(value => value.Id == request.EncuentroId
                && (value.UsuarioId == null || value.UsuarioId == userId), cancellationToken);
        if (currentEncounter is null || currentEncounter.FechaExpiracion <= DateTime.UtcNow)
            return Conflict("El encuentro ya terminó o expiró.");

        var userState = await context.EncuentrosUsuario.FirstOrDefaultAsync(value =>
            value.EncuentroId == currentEncounter.Id && value.UsuarioId == userId, cancellationToken);
        if (userState?.Capturado == true)
            return Conflict("Ya capturaste tu copia de este Pokémon.");
        if (userState is null)
        {
            userState = new EncuentroUsuario { EncuentroId = currentEncounter.Id, UsuarioId = userId };
            context.EncuentrosUsuario.Add(userState);
        }

        var inventory = await context.ItemUsers.Include(entry => entry.Item)
            .ThenInclude(item => item.EfectoJuego)
            .SingleOrDefaultAsync(entry => entry.IdUser == userId && entry.IdItem == request.PokeballItemId,
                cancellationToken);
        if (inventory is null || inventory.Cantidad <= 0)
            return Conflict("No tienes disponible ese objeto.");
        if (inventory.Item.EfectoJuego?.Tipo != "ball" || inventory.Item.EfectoJuego.MultiplicadorCaptura is null)
            return BadRequest("El ítem indicado no es una Poké Ball utilizable.");

        var now = DateTime.UtcNow;
        var ballMultiplier = inventory.Item.EfectoJuego.MultiplicadorCaptura.Value;
        var noTeVio = !userState.JugadorDetectado;
        var situationMultiplier = 1f;
        var backThrow = userState.TiroPorLaEspaldaDisponible && !userState.JugadorDetectado;
        if (backThrow)
        {
            situationMultiplier *= 2f;
        }
        else if (!userState.JugadorDetectado)
        {
            situationMultiplier *= 1.5f;
        }
        var eating = userState.ComiendoHasta is { } eatingUntil && eatingUntil > now;
        if (eating) situationMultiplier *= 1.5f;
        if (currentEncounter.Dormido) situationMultiplier *= 2f;

        var quality = userState.CalidadTiroDisponible;
        var qualityMultiplier = quality switch
        {
            "bueno" => 1.2f,
            "excelente" => 1.5f,
            "perfecto" => 2f,
            _ => 1f
        };
        situationMultiplier *= qualityMultiplier;

        var normalCaptureChance = Math.Clamp(
            pokemon.CaptureRate / 255d * ballMultiplier * situationMultiplier, 0d, 1d);
        var speciesCaught = await context.Pokedex.Where(entry => entry.UserId == userId)
            .Select(entry => entry.PokemonId).Distinct().CountAsync(cancellationToken);
        var criticalChance = Math.Min(speciesCaught * .02d, .25d);
        var critical = Random.Shared.NextDouble() < criticalChance;
        var success = critical || Random.Shared.NextDouble() < normalCaptureChance;
        // El lanzamiento revela al jugador, de modo que el bonus sorpresa solo aplica al primer tiro.
        if (noTeVio)
        {
            userState.JugadorDetectado = true;
            userState.TiroPorLaEspaldaDisponible = false;
        }
        userState.CalidadTiroDisponible = "normal";

        inventory.Cantidad--;
        var capture = new Captura
        {
            UsuarioId = userId,
            EncuentroId = currentEncounter.Id,
            PokemonId = pokemon.Id,
            PokemonNombre = pokemon.Nombre,
            IdPokeball = inventory.IdItem,
            Exitosa = success,
            ProbabilidadAplicada = (float)normalCaptureChance,
            CaptureRate = pokemon.CaptureRate,
            MultiplicadorBola = ballMultiplier,
            MultiplicadorSituacional = situationMultiplier,
            NoTeVio = noTeVio,
            TiroPorLaEspalda = backThrow,
            PokemonComiendo = eating,
            PokemonDormido = currentEncounter.Dormido,
            ProbabilidadCritica = (float)criticalChance,
            FueCritica = critical,
            CalidadTiro = quality,
            Fecha = now
        };
        context.Capturas.Add(capture);

        PokemonUser? capturedPokemon = null;
        if (success)
        {
            userState.Capturado = true;
            userState.FechaCaptura = now;
            capturedPokemon = new PokemonUser
            {
                IdPokemon = pokemon.Id,
                IdUsuario = userId,
                Nombre = pokemon.Nombre,
                EstadoDetalle = new PokemonEstado
                {
                    VidaTotal = pokemon.Vida,
                    VidaActual = pokemon.Vida,
                    Estado = EstadoPokemon.Sano,
                    TurnosEstado = 0,
                    DuracionEstado = 0,
                    IdPokeball = inventory.IdItem,
                    AtaqueBase = pokemon.Ataque,
                    DefensaBase = pokemon.Defensa,
                    VelocidadBase = pokemon.Velocidad,
                    TipoPrimario = pokemon.TipoPrimario,
                    TipoSecundario = pokemon.TipoSecundario,
                    UrlFrontal = pokemon.UrlFrontal,
                    UrlTrasera = pokemon.UrlTrasera
                }
            };
            capture.PokemonUser = capturedPokemon;
            context.PokemonUsers.Add(capturedPokemon);

            if (!await context.Pokedex.AnyAsync(entry => entry.UserId == userId && entry.PokemonId == pokemon.Id,
                cancellationToken))
            {
                context.Pokedex.Add(new Pokedex { UserId = userId, PokemonId = pokemon.Id, Fecha = now });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new ResultadoCapturaDto(capture.Id, success, inventory.Cantidad, ToDto(capture),
            capturedPokemon is null ? null : ToDto(capturedPokemon),
            critical ? "¡Captura crítica!" : success ? null : "El Pokémon escapó."));
    }

    [HttpPost("/api/encuentros/{id:int}/bayas")]
    public async Task<IActionResult> UsarBaya(int id, UsarBayaDto request, CancellationToken cancellationToken)
    {
        var userId = UserId;
        if (userId is null) return Unauthorized();
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var encounter = await context.Encuentros.FirstOrDefaultAsync(value => value.Id == id
            && (value.UsuarioId == null || value.UsuarioId == userId), cancellationToken);
        if (encounter is null) return NotFound();
        var now = DateTime.UtcNow;
        if (encounter.FechaExpiracion <= now)
            return Conflict("El encuentro ya terminó o expiró.");
        var userState = await context.EncuentrosUsuario.FirstOrDefaultAsync(value =>
            value.EncuentroId == id && value.UsuarioId == userId, cancellationToken);
        if (userState?.Capturado == true) return Conflict("Ya capturaste tu copia de este Pokémon.");
        if (userState?.ComiendoHasta > now) return Conflict("El Pokémon ya está comiendo una baya.");

        var inventory = await context.ItemUsers.Include(entry => entry.Item).ThenInclude(item => item.EfectoJuego)
            .SingleOrDefaultAsync(entry => entry.IdUser == userId && entry.IdItem == request.BayaItemId,
                cancellationToken);
        if (inventory is null || inventory.Cantidad <= 0) return Conflict("No tienes disponible esa baya.");
        var effect = inventory.Item.EfectoJuego;
        if (effect?.Tipo != "berry") return BadRequest("El ítem indicado no es una baya de captura.");

        inventory.Cantidad--;
        if (userState is null)
        {
            userState = new EncuentroUsuario { EncuentroId = id, UsuarioId = userId };
            context.EncuentrosUsuario.Add(userState);
        }
        userState.ComiendoHasta = now.AddSeconds(effect.DuracionSegundos ?? 30);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { encuentroId = id, comiendoHasta = userState.ComiendoHasta, cantidadRestante = inventory.Cantidad });
    }

    [HttpGet("/api/me/capturas")]
    public async Task<IActionResult> GetMine([FromQuery] bool? success, CancellationToken cancellationToken)
    {
        if (UserId is null) return Unauthorized();
        var query = context.Capturas.AsNoTracking().Where(capture => capture.UsuarioId == UserId);
        if (success.HasValue) query = query.Where(capture => capture.Exitosa == success.Value);
        return Ok(await query.OrderByDescending(capture => capture.Fecha).ThenByDescending(capture => capture.Id)
            .Select(capture => new CapturaDto(capture.Id, capture.EncuentroId, capture.PokemonId, capture.PokemonNombre,
                capture.IdPokeball, capture.Exitosa, capture.ProbabilidadAplicada, capture.IdPokemonUser,
                capture.Fecha, capture.CaptureRate, capture.MultiplicadorBola, capture.MultiplicadorSituacional,
                capture.NoTeVio, capture.TiroPorLaEspalda, capture.PokemonComiendo, capture.PokemonDormido,
                capture.ProbabilidadCritica, capture.FueCritica, capture.CalidadTiro))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("/api/me/capturas/{id:int}")]
    public async Task<IActionResult> GetMineById(int id, CancellationToken cancellationToken)
    {
        if (UserId is null) return Unauthorized();
        var capture = await context.Capturas.AsNoTracking().FirstOrDefaultAsync(
            value => value.Id == id && value.UsuarioId == UserId, cancellationToken);
        return capture is null ? NotFound() : Ok(ToDto(capture));
    }

    private static CapturaDto ToDto(Captura capture) => new(capture.Id, capture.EncuentroId, capture.PokemonId,
        capture.PokemonNombre, capture.IdPokeball, capture.Exitosa, capture.ProbabilidadAplicada,
        capture.IdPokemonUser, capture.Fecha, capture.CaptureRate, capture.MultiplicadorBola,
        capture.MultiplicadorSituacional, capture.NoTeVio, capture.TiroPorLaEspalda,
        capture.PokemonComiendo, capture.PokemonDormido, capture.ProbabilidadCritica,
        capture.FueCritica, capture.CalidadTiro);

    private static PokemonCapturadoDto ToDto(PokemonUser pokemon) => new(pokemon.Id, pokemon.IdPokemon,
        pokemon.Nombre ?? pokemon.IdPokemon.ToString(), pokemon.EstadoDetalle!.VidaActual,
        pokemon.EstadoDetalle.VidaTotal, pokemon.EstadoDetalle.Estado.ToString(),
        pokemon.EstadoDetalle.TipoPrimario, pokemon.EstadoDetalle.TipoSecundario,
        pokemon.EstadoDetalle.UrlFrontal, pokemon.EstadoDetalle.UrlTrasera);
}
