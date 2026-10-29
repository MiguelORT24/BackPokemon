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
[Route("api/[controller]")]
public class BatallasController(ApplicationDbContext context, BattleRewardService rewards, IConfiguration configuration) : ControllerBase
{
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        if (UserId is null) return Unauthorized();
        return Ok(await context.Batallas.AsNoTracking().Include(value => value.Recompensa).ThenInclude(value => value!.Item)
            .Where(value => value.IdUsuario == UserId).OrderByDescending(value => value.Fecha).ThenByDescending(value => value.Id)
            .Select(value => ToDto(value)).ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        if (UserId is null) return Unauthorized();
        var battle = await context.Batallas.AsNoTracking().Include(value => value.Recompensa).ThenInclude(value => value!.Item)
            .FirstOrDefaultAsync(value => value.Id == id && value.IdUsuario == UserId, cancellationToken);
        return battle is null ? NotFound() : Ok(ToDto(battle));
    }

    [HttpGet("usuario/{userId}")]
    public async Task<IActionResult> GetByUsuario(string userId, [FromQuery] ResultadoBatalla? resultado,
        [FromQuery] int pagina = 1, [FromQuery] int tamano = 20, CancellationToken cancellationToken = default)
    {
        if (pagina < 1 || tamano is < 1 or > 100) return BadRequest("pagina debe ser >= 1 y tamano debe estar entre 1 y 100.");
        if (resultado.HasValue && !Enum.IsDefined(resultado.Value)) return BadRequest("resultado no válido.");
        var query = context.Batallas.AsNoTracking().Where(value => value.IdUsuario == userId);
        if (resultado.HasValue) query = query.Where(value => value.Resultado == resultado.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(value => value.Fecha).ThenByDescending(value => value.Id)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .Select(value => new BatallaHistorialDto(value.Id, value.Fecha, value.Resultado, value.IdPokemonRival,
                new PokemonBatallaDto(value.PokemonUserRetador.Id, value.PokemonUserRetador.IdPokemon,
                    value.PokemonUserRetador.Nombre, value.PokemonUserRetador.EstadoDetalle == null
                        ? null : value.PokemonUserRetador.EstadoDetalle.UrlFrontal)))
            .ToListAsync(cancellationToken);
        return Ok(new { total, pagina, tamano, items });
    }

    [HttpGet("usuario/{userId}/resumen")]
    public async Task<IActionResult> GetByUsuarioResumen(string userId, CancellationToken cancellationToken)
    {
        if (!await context.Users.AnyAsync(value => value.Id == userId, cancellationToken)) return NotFound();
        var battles = context.Batallas.AsNoTracking().Where(value => value.IdUsuario == userId);
        var aggregate = await battles.GroupBy(_ => 1).Select(group => new
        {
            Total = group.Count(),
            Victorias = group.Count(value => value.Resultado == ResultadoBatalla.Victoria),
            Derrotas = group.Count(value => value.Resultado == ResultadoBatalla.Derrota),
            Huidas = group.Count(value => value.Resultado == ResultadoBatalla.Huida)
        }).SingleOrDefaultAsync(cancellationToken);
        var total = aggregate?.Total ?? 0;
        var wins = aggregate?.Victorias ?? 0;
        var losses = aggregate?.Derrotas ?? 0;
        var decisions = wins + losses;
        var recent = await battles.OrderByDescending(value => value.Fecha).ThenByDescending(value => value.Id)
            .Select(value => value.Resultado).ToListAsync(cancellationToken);
        var streak = recent.Count == 0 ? null : new RachaBatallaDto(recent[0], recent.TakeWhile(value => value == recent[0]).Count());
        var bestPokemon = await battles.Where(value => value.Resultado == ResultadoBatalla.Victoria)
            .GroupBy(value => new { value.PokemonUserR, value.PokemonUserRetador.IdPokemon, value.PokemonUserRetador.Nombre })
            .Select(group => new { group.Key.PokemonUserR, group.Key.IdPokemon, group.Key.Nombre, Victorias = group.Count() })
            .OrderByDescending(value => value.Victorias).ThenBy(value => value.PokemonUserR)
            .Select(value => new MejorPokemonBatallaDto(value.PokemonUserR, value.IdPokemon, value.Nombre, value.Victorias))
            .FirstOrDefaultAsync(cancellationToken);
        var rival = await battles.GroupBy(value => value.IdPokemonRival)
            .Select(group => new RivalBatallaDto(group.Key, group.Count()))
            .OrderByDescending(value => value.Veces).ThenBy(value => value.IdPokemonRival)
            .FirstOrDefaultAsync(cancellationToken);
        return Ok(new
        {
            total,
            victorias = wins,
            derrotas = losses,
            huidas = aggregate?.Huidas ?? 0,
            winRate = decisions == 0 ? (double?)null : (double)wins / decisions,
            rachaActual = streak,
            mejorPokemon = bestPokemon,
            rivalMasEnfrentado = rival
        });
    }

    // Player-reported result; it does not award items because the client result is not independently verifiable.
    // The old body remains accepted only for GameAdmin during the client transition.
    [HttpPost]
    public async Task<IActionResult> Create(CreateBatallaDto request, CancellationToken cancellationToken)
    {
        var userId = UserId;
        if (userId is null) return Unauthorized();
        if (request.Resultado is null)
        {
            if (!User.IsInRole("GameAdmin")) return Forbid();
            if (request.PokemonUserGanador is null || request.LlaveIdempotencia is null)
                return BadRequest("El formato anterior requiere pokemonUserGanador y llaveIdempotencia.");
            return await CreateWithReward(new CreateBatallaConRecompensaDto
            {
                LlaveIdempotencia = request.LlaveIdempotencia.Value,
                PokemonUserR = request.PokemonUserR,
                IdPokemonRival = request.IdPokemonRival,
                PokemonUserGanador = request.PokemonUserGanador.Value
            }, cancellationToken);
        }
        if (request.PokemonUserGanador.HasValue)
            return BadRequest("Envía resultado o el formato anterior, no ambos.");
        if (!Enum.IsDefined(request.Resultado.Value)) return BadRequest("resultado no válido.");
        var maximumSpecies = Math.Max(1, configuration.GetValue("Game:TotalSpecies", 151));
        if (request.IdPokemonRival > maximumSpecies)
            return BadRequest($"idPokemonRival debe estar entre 1 y {maximumSpecies}.");
        if (request.LlaveIdempotencia is { } key && key != Guid.Empty)
        {
            var existing = await context.Batallas.AsNoTracking().FirstOrDefaultAsync(value => value.LlaveIdempotencia == key,
                cancellationToken);
            if (existing is not null)
                return existing.IdUsuario == userId ? Ok(ToDto(existing)) : Conflict("La llave ya pertenece a otra batalla.");
        }
        var challenger = await context.PokemonUsers.AsNoTracking()
            .FirstOrDefaultAsync(value => value.Id == request.PokemonUserR, cancellationToken);
        if (challenger is null) return NotFound("El Pokémon retador no existe.");
        if (challenger.IdUsuario != userId) return Forbid();

        var battle = new Batalla
        {
            IdUsuario = userId,
            PokemonUserR = request.PokemonUserR,
            IdPokemonRival = request.IdPokemonRival,
            Resultado = request.Resultado.Value,
            Fecha = DateTime.UtcNow,
            LlaveIdempotencia = request.LlaveIdempotencia is { } value && value != Guid.Empty ? value : null
        };
        context.Batallas.Add(battle);
        await context.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = battle.Id }, ToDto(battle));
    }

    // Keeps the old reward flow for trusted callers while normal player reports cannot mint items.
    [Authorize(Roles = "GameAdmin")]
    [HttpPost("recompensa")]
    public async Task<IActionResult> CreateWithReward(CreateBatallaConRecompensaDto request,
        CancellationToken cancellationToken)
    {
        var userId = UserId;
        if (userId is null) return Unauthorized();
        if (request.LlaveIdempotencia == Guid.Empty) return BadRequest("LlaveIdempotencia es obligatoria.");
        if (request.IdPokemonRival > Math.Max(1, configuration.GetValue("Game:TotalSpecies", 151)))
            return BadRequest("idPokemonRival fuera del rango configurado.");
        var existing = await context.Batallas.AsNoTracking().Include(value => value.Recompensa).ThenInclude(value => value!.Item)
            .FirstOrDefaultAsync(value => value.LlaveIdempotencia == request.LlaveIdempotencia, cancellationToken);
        if (existing is not null)
            return existing.IdUsuario == userId ? Ok(ToDto(existing)) : Conflict("La llave ya pertenece a otra batalla.");
        var challenger = await context.PokemonUsers.AsNoTracking().FirstOrDefaultAsync(
            value => value.Id == request.PokemonUserR, cancellationToken);
        if (challenger is null) return NotFound("El Pokémon retador no existe.");
        if (challenger.IdUsuario != userId) return Forbid();
        if (!await context.PokemonUsers.AnyAsync(value => value.Id == request.PokemonUserGanador, cancellationToken))
            return NotFound("El Pokémon ganador no existe.");
        var rewardPoolReady = await context.EfectosItems.AnyAsync(effect => effect.Tipo == "ball" || effect.Tipo == "berry",
            cancellationToken);
        if (!rewardPoolReady) return Conflict("Sincroniza el catálogo y las reglas de objetos antes de entregar premios.");

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var battle = new Batalla
        {
            IdUsuario = userId,
            PokemonUserR = request.PokemonUserR,
            IdPokemonRival = request.IdPokemonRival,
            Resultado = request.PokemonUserGanador == request.PokemonUserR
                ? ResultadoBatalla.Victoria : ResultadoBatalla.Derrota,
            Fecha = DateTime.UtcNow,
            LlaveIdempotencia = request.LlaveIdempotencia
        };
        context.Batallas.Add(battle);
        await context.SaveChangesAsync(cancellationToken);
        var reward = await rewards.AwardRandomItem(battle.Id, userId, cancellationToken);
        if (reward is null) return Conflict("No hay objetos disponibles para la recompensa.");
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await context.Entry(battle).Reference(value => value.Recompensa).LoadAsync(cancellationToken);
        if (battle.Recompensa is not null)
            await context.Entry(battle.Recompensa).Reference(value => value.Item).LoadAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ToDto(battle));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateBatallaDto request, CancellationToken cancellationToken)
    {
        var userId = UserId;
        if (userId is null) return Unauthorized();
        if (!Enum.IsDefined(request.Resultado)) return BadRequest("resultado no válido.");
        var battle = await context.Batallas.FirstOrDefaultAsync(value => value.Id == id && value.IdUsuario == userId,
            cancellationToken);
        if (battle is null) return NotFound();
        if (await context.RecompensasBatalla.AnyAsync(value => value.BatallaId == id, cancellationToken))
            return Conflict("No se puede editar una batalla que ya entregó su recompensa.");
        if (!await context.PokemonUsers.AnyAsync(value => value.Id == request.PokemonUserR && value.IdUsuario == userId,
            cancellationToken)) return Forbid();
        if (request.IdPokemonRival > configuration.GetValue("Game:TotalSpecies", 151))
            return BadRequest("idPokemonRival fuera del rango configurado.");
        battle.PokemonUserR = request.PokemonUserR;
        battle.IdPokemonRival = request.IdPokemonRival;
        battle.Resultado = request.Resultado;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = UserId;
        if (userId is null) return Unauthorized();
        var battle = await context.Batallas.FirstOrDefaultAsync(value => value.Id == id && value.IdUsuario == userId,
            cancellationToken);
        if (battle is null) return NotFound();
        if (await context.RecompensasBatalla.AnyAsync(value => value.BatallaId == id, cancellationToken))
            return Conflict("No se puede eliminar una batalla que ya entregó su recompensa.");
        context.Batallas.Remove(battle);
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static BatallaDto ToDto(Batalla battle) => new()
    {
        Id = battle.Id, IdUsuario = battle.IdUsuario, PokemonUserR = battle.PokemonUserR,
        IdPokemonRival = battle.IdPokemonRival, Resultado = battle.Resultado, Fecha = battle.Fecha,
        LlaveIdempotencia = battle.LlaveIdempotencia,
        Recompensa = battle.Recompensa is null ? null : new ItemDto
        {
            Id = battle.Recompensa.Item.Id, PokeApiItemId = battle.Recompensa.Item.PokeApiItemId,
            Nombre = battle.Recompensa.Item.Nombre, Probabilidad = battle.Recompensa.Item.Probabilidad,
            UrlImagen = battle.Recompensa.Item.UrlImagen, Descripcion = battle.Recompensa.Item.Descripcion,
            Efecto = battle.Recompensa.Item.Efecto, Valor = battle.Recompensa.Item.Valor
        },
        CantidadRecompensa = battle.Recompensa?.Cantidad
    };
}