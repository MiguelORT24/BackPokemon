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
        if (UserId is null) return Unauthorized();

        PokeApiPokemon pokemon;
        try
        {
            pokemon = await pokeApi.GetPokemon(request.PokemonId, cancellationToken);
        }
        catch (PokeApiPokemonNotFoundException)
        {
            return NotFound("El Pokémon indicado no existe en PokéAPI.");
        }
        catch (PokeApiUnavailableException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "PokeAPI no pudo completar la consulta.");
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "No se pudo conectar con PokeAPI.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var inventory = await context.ItemUsers.Include(entry => entry.Item)
            .SingleOrDefaultAsync(entry => entry.IdUser == UserId && entry.IdItem == request.PokeballItemId,
                cancellationToken);
        if (inventory is null || inventory.Cantidad <= 0)
            return Conflict("No tienes disponible esa Poké Ball.");
        if (!string.Equals(inventory.Item.Efecto, "captura", StringComparison.OrdinalIgnoreCase))
            return BadRequest("El ítem indicado no sirve para capturar Pokémon.");
        if (inventory.Item.Probabilidad is < 0 or > 1)
            return Conflict("La Poké Ball tiene una probabilidad de captura inválida.");

        inventory.Cantidad--;
        var success = Random.Shared.NextDouble() < inventory.Item.Probabilidad;
        var capture = new Captura
        {
            UsuarioId = UserId,
            PokemonId = pokemon.Id,
            PokemonNombre = pokemon.Nombre,
            IdPokeball = inventory.IdItem,
            Exitosa = success,
            ProbabilidadAplicada = inventory.Item.Probabilidad,
            Fecha = DateTime.UtcNow
        };
        context.Capturas.Add(capture);

        PokemonUser? capturedPokemon = null;
        if (success)
        {
            capturedPokemon = new PokemonUser
            {
                IdPokemon = pokemon.Id,
                IdUsuario = UserId,
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

            if (!await context.Pokedex.AnyAsync(entry => entry.UserId == UserId && entry.PokemonId == pokemon.Id,
                cancellationToken))
            {
                context.Pokedex.Add(new Pokedex
                {
                    UserId = UserId,
                    PokemonId = pokemon.Id,
                    Fecha = DateTime.UtcNow
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var captureDto = ToDto(capture);
        return Ok(new ResultadoCapturaDto(
            capture.Id, success, inventory.Cantidad, captureDto,
            capturedPokemon is null ? null : ToDto(capturedPokemon),
            success ? null : "El Pokémon escapó."));
    }

    [HttpGet("/api/me/capturas")]
    public async Task<IActionResult> GetMine([FromQuery] bool? success, CancellationToken cancellationToken)
    {
        if (UserId is null) return Unauthorized();
        var query = context.Capturas.AsNoTracking().Where(capture => capture.UsuarioId == UserId);
        if (success.HasValue) query = query.Where(capture => capture.Exitosa == success.Value);
        var captures = await query.OrderByDescending(capture => capture.Fecha).ThenByDescending(capture => capture.Id)
            .Select(capture => new CapturaDto(capture.Id, capture.PokemonId, capture.PokemonNombre,
                capture.IdPokeball, capture.Exitosa, capture.ProbabilidadAplicada,
                capture.IdPokemonUser, capture.Fecha))
            .ToListAsync(cancellationToken);
        return Ok(captures);
    }

    [HttpGet("/api/me/capturas/{id:int}")]
    public async Task<IActionResult> GetMineById(int id, CancellationToken cancellationToken)
    {
        if (UserId is null) return Unauthorized();
        var capture = await context.Capturas.AsNoTracking().FirstOrDefaultAsync(
            value => value.Id == id && value.UsuarioId == UserId, cancellationToken);
        return capture is null ? NotFound() : Ok(ToDto(capture));
    }

    private static CapturaDto ToDto(Captura capture) => new(capture.Id, capture.PokemonId,
        capture.PokemonNombre, capture.IdPokeball, capture.Exitosa, capture.ProbabilidadAplicada,
        capture.IdPokemonUser, capture.Fecha);

    private static PokemonCapturadoDto ToDto(PokemonUser pokemon) => new(pokemon.Id, pokemon.IdPokemon,
        pokemon.Nombre ?? pokemon.IdPokemon.ToString(), pokemon.EstadoDetalle!.VidaActual,
        pokemon.EstadoDetalle.VidaTotal, pokemon.EstadoDetalle.Estado.ToString(),
        pokemon.EstadoDetalle.TipoPrimario, pokemon.EstadoDetalle.TipoSecundario,
        pokemon.EstadoDetalle.UrlFrontal, pokemon.EstadoDetalle.UrlTrasera);
}
