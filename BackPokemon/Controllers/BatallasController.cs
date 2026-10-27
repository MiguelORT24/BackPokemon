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
public class BatallasController(ApplicationDbContext context, BattleRewardService rewards) : ControllerBase
{
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BatallaDto>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = UserId;
        if (userId is null) return Unauthorized();
        var battles = await context.Batallas.AsNoTracking().Include(value => value.Recompensa).ThenInclude(value => value!.Item)
            .Where(value => value.PokemonUserRetador.IdUsuario == userId)
            .Select(value => ToDto(value)).ToListAsync(cancellationToken);
        return Ok(battles);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BatallaDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var userId = UserId;
        if (userId is null) return Unauthorized();
        var battle = await context.Batallas.AsNoTracking().Include(value => value.Recompensa).ThenInclude(value => value!.Item)
            .FirstOrDefaultAsync(value => value.Id == id && value.PokemonUserRetador.IdUsuario == userId,
                cancellationToken);
        return battle is null ? NotFound() : Ok(ToDto(battle));
    }

    [HttpGet("usuario/{userId}")]
    public async Task<ActionResult<IEnumerable<BatallaDto>>> GetByUsuario(string userId,
        CancellationToken cancellationToken)
    {
        if (userId != UserId) return NotFound();
        var battles = await context.Batallas.AsNoTracking().Include(value => value.Recompensa).ThenInclude(value => value!.Item)
            .Where(value => value.PokemonUserRetador.IdUsuario == userId)
            .Select(value => ToDto(value)).ToListAsync(cancellationToken);
        return Ok(battles);
    }

    // Creating a battle row represents a completed battle and grants exactly one item.
    // The client must reuse LlaveIdempotencia when retrying the same completion request.
    [Authorize(Roles = "GameAdmin")]
    [HttpPost]
    public async Task<ActionResult<BatallaDto>> Create(CreateBatallaDto request,
        CancellationToken cancellationToken)
    {
        var userId = UserId;
        if (userId is null) return Unauthorized();
        if (request.LlaveIdempotencia == Guid.Empty) return BadRequest("LlaveIdempotencia es obligatoria.");

        var existing = await context.Batallas.AsNoTracking().Include(value => value.Recompensa)
            .ThenInclude(value => value!.Item)
            .FirstOrDefaultAsync(value => value.LlaveIdempotencia == request.LlaveIdempotencia, cancellationToken);
        if (existing is not null)
        {
            if (existing.PokemonUserRetador.IdUsuario != userId) return Conflict("La llave ya pertenece a otra batalla.");
            return Ok(ToDto(existing));
        }

        var challenger = await context.PokemonUsers.AsNoTracking()
            .FirstOrDefaultAsync(value => value.Id == request.PokemonUserR, cancellationToken);
        if (challenger is null) return NotFound("El Pokémon retador no existe.");
        if (challenger.IdUsuario != userId) return Forbid();
        if (!await context.PokemonUsers.AnyAsync(value => value.Id == request.PokemonUserGanador, cancellationToken))
            return NotFound("El Pokémon ganador no existe.");
        var rewardPoolReady = await context.EfectosItems.AnyAsync(effect => effect.Tipo == "ball" || effect.Tipo == "berry",
            cancellationToken);
        if (!rewardPoolReady) return Conflict("Sincroniza el catálogo y las reglas de objetos antes de entregar premios.");

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        var battle = new Batalla
        {
            PokemonUserR = request.PokemonUserR, IdPokemonRival = request.IdPokemonRival,
            Fecha = request.Fecha == default ? DateTime.UtcNow : request.Fecha,
            PokemonUserGanador = request.PokemonUserGanador,
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
        var response = ToDto(battle);
        return CreatedAtAction(nameof(GetById), new { id = battle.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateBatallaDto request, CancellationToken cancellationToken)
    {
        var userId = UserId;
        if (userId is null) return Unauthorized();
        var battle = await context.Batallas.FirstOrDefaultAsync(value => value.Id == id
            && value.PokemonUserRetador.IdUsuario == userId, cancellationToken);
        if (battle is null) return NotFound();
        if (await context.RecompensasBatalla.AnyAsync(value => value.BatallaId == id, cancellationToken))
            return Conflict("No se puede editar una batalla que ya entregó su recompensa.");
        if (!await PokemonUsersExist(request, cancellationToken)) return NotFound("Uno o más PokemonUser no existen.");
        if (!await context.PokemonUsers.AnyAsync(value => value.Id == request.PokemonUserR
            && value.IdUsuario == userId, cancellationToken)) return Forbid();
        battle.PokemonUserR = request.PokemonUserR;
        battle.IdPokemonRival = request.IdPokemonRival;
        battle.Fecha = request.Fecha;
        battle.PokemonUserGanador = request.PokemonUserGanador;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = UserId;
        if (userId is null) return Unauthorized();
        var battle = await context.Batallas.FirstOrDefaultAsync(value => value.Id == id
            && value.PokemonUserRetador.IdUsuario == userId, cancellationToken);
        if (battle is null) return NotFound();
        if (await context.RecompensasBatalla.AnyAsync(value => value.BatallaId == id, cancellationToken))
            return Conflict("No se puede eliminar una batalla que ya entregó su recompensa.");
        context.Batallas.Remove(battle);
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<bool> PokemonUsersExist(UpdateBatallaDto request, CancellationToken cancellationToken)
    {
        var ids = new[] { request.PokemonUserR, request.PokemonUserGanador };
        var count = await context.PokemonUsers.Where(value => ids.Contains(value.Id)).Select(value => value.Id)
            .Distinct().CountAsync(cancellationToken);
        return count == ids.Distinct().Count();
    }

    private static BatallaDto ToDto(Batalla battle) => new()
    {
        Id = battle.Id, PokemonUserR = battle.PokemonUserR, IdPokemonRival = battle.IdPokemonRival,
        Fecha = battle.Fecha, PokemonUserGanador = battle.PokemonUserGanador,
        LlaveIdempotencia = battle.LlaveIdempotencia ?? Guid.Empty,
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
