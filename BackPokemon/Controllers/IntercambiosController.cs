using System.Data;
using System.Security.Claims;
using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using BackPokemon.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Controllers;

[Authorize]
[ApiController]
[Route("trades")]
[Route("api/trades")]
public class IntercambiosController(ApplicationDbContext context) : ControllerBase
{
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private IQueryable<Intercambio> Details => context.Intercambios
        .Include(t => t.UsuarioCreador).Include(t => t.UsuarioDestinatario)
        .Include(t => t.PokemonUserRemitente).Include(t => t.PokemonUserDestinatario);

    [HttpGet("/me/trades")]
    [HttpGet("/api/me/trades")]
    public async Task<IActionResult> Mine([FromQuery] string? status, CancellationToken ct)
    {
        if (UserId is null) return Unauthorized();
        if (status is not null && status is not ("pending" or "accepted" or "confirmed" or "cancelled" or "rejected"))
            return BadRequest("status no válido.");
        var query = Details.AsNoTracking().Where(t => t.UsuarioCreadorId == UserId || t.UsuarioDestinatarioId == UserId);
        if (status is not null) query = query.Where(t => t.Estado == status);
        var trades = await query.OrderByDescending(t => t.Fecha).ThenByDescending(t => t.Id).ToListAsync(ct);
        return Ok(trades.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        if (UserId is null) return Unauthorized();
        var trade = await Details.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id
            && (t.UsuarioCreadorId == UserId || t.UsuarioDestinatarioId == UserId), ct);
        return trade is null ? NotFound() : Ok(ToDto(trade));
    }

    [HttpPost]
    public Task<IActionResult> Create(CreateTradeDto request, CancellationToken ct) => Mutate(async () =>
    {
        if (request.RecipientUserId == UserId) return BadRequest("No puedes invitarte a ti mismo.");
        if (!await context.Users.AnyAsync(u => u.Id == request.RecipientUserId, ct))
            return NotFound("El destinatario no existe.");
        if (!await context.AvailablePokemon().AnyAsync(p => p.Id == request.PokemonUserId && p.IdUsuario == UserId, ct))
            return Conflict("El Pokémon no te pertenece, no está vivo o está reservado.");
        var trade = new Intercambio
        {
            UsuarioCreadorId = UserId!, UsuarioDestinatarioId = request.RecipientUserId,
            PokemonUserR = request.PokemonUserId, Estado = "pending", Fecha = DateTime.UtcNow
        };
        context.Intercambios.Add(trade);
        await context.SaveChangesAsync(ct);
        await Details.FirstAsync(t => t.Id == trade.Id, ct);
        return Created($"/trades/{trade.Id}", ToDto(trade));
    }, ct);

    [HttpPost("{id:int}/accept")]
    public Task<IActionResult> Accept(int id, AcceptTradeDto request, CancellationToken ct) => Mutate(async () =>
    {
        var trade = await FindParticipant(id, ct);
        if (trade is null) return NotFound();
        if (trade.UsuarioDestinatarioId != UserId) return Forbid();
        if (trade.Estado != "pending") return Conflict("Solo se puede aceptar un intercambio pending.");
        if (request.PokemonUserId == trade.PokemonUserR ||
            !await context.AvailablePokemon().AnyAsync(p => p.Id == request.PokemonUserId && p.IdUsuario == UserId, ct))
            return Conflict("El Pokémon no te pertenece, no está vivo o está reservado.");
        if (!await context.AvailablePokemon(id).AnyAsync(p => p.Id == trade.PokemonUserR
            && p.IdUsuario == trade.UsuarioCreadorId, ct))
            return Conflict("El Pokémon del creador ya no está disponible.");
        trade.PokemonUserD = request.PokemonUserId;
        trade.Estado = "accepted";
        trade.FechaAceptacion = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        await context.Entry(trade).Reference(t => t.PokemonUserDestinatario).LoadAsync(ct);
        return Ok(ToDto(trade));
    }, ct);

    [HttpPost("{id:int}/confirm")]
    public Task<IActionResult> Confirm(int id, CancellationToken ct) => Mutate(async () =>
    {
        var trade = await FindParticipant(id, ct);
        if (trade is null) return NotFound();
        if (trade.UsuarioCreadorId != UserId) return Forbid();
        if (trade.Estado != "accepted" || trade.PokemonUserD is null)
            return Conflict("Solo se puede confirmar un intercambio accepted.");
        var valid = await context.AvailablePokemon(id).CountAsync(p =>
            (p.Id == trade.PokemonUserR && p.IdUsuario == trade.UsuarioCreadorId) ||
            (p.Id == trade.PokemonUserD && p.IdUsuario == trade.UsuarioDestinatarioId), ct);
        if (valid != 2) return Conflict("Los Pokémon ya no están disponibles o cambiaron de propietario.");
        trade.PokemonUserRemitente.IdUsuario = trade.UsuarioDestinatarioId;
        trade.PokemonUserDestinatario!.IdUsuario = trade.UsuarioCreadorId;
        trade.Estado = "confirmed";
        trade.FechaConfirmacion = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return Ok(ToDto(trade));
    }, ct);

    [HttpPost("{id:int}/cancel")]
    public Task<IActionResult> Cancel(int id, CancellationToken ct) => Mutate(async () =>
    {
        var trade = await FindParticipant(id, ct);
        if (trade is null) return NotFound();
        if (trade.Estado is not ("pending" or "accepted"))
            return Conflict("El intercambio ya terminó.");
        trade.Estado = trade.Estado == "pending" && trade.UsuarioDestinatarioId == UserId ? "rejected" : "cancelled";
        await context.SaveChangesAsync(ct);
        return Ok(ToDto(trade));
    }, ct);

    private Task<Intercambio?> FindParticipant(int id, CancellationToken ct) =>
        Details.FirstOrDefaultAsync(t => t.Id == id
            && (t.UsuarioCreadorId == UserId || t.UsuarioDestinatarioId == UserId), ct);

    // SERIALIZABLE protege también la ausencia de reservas frente a solicitudes simultáneas.
    private async Task<IActionResult> Mutate(Func<Task<IActionResult>> action, CancellationToken ct)
    {
        if (UserId is null) return Unauthorized();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var result = await action();
            if (result is ObjectResult { StatusCode: >= 400 } || result is StatusCodeResult { StatusCode: >= 400 }
                || result is ForbidResult)
                await transaction.RollbackAsync(ct);
            else
                await transaction.CommitAsync(ct);
            return result;
        }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException ||
            ex is SqlException { Number: 1205 or 2601 or 2627 } ||
            ex is DbUpdateException { InnerException: SqlException { Number: 1205 or 2601 or 2627 } })
        {
            await transaction.RollbackAsync(ct);
            return Conflict("El intercambio cambió en otra solicitud. Vuelve a consultar e intenta de nuevo.");
        }
    }

    private TradeDto ToDto(Intercambio t) => new(t.Id,
        t.UsuarioCreadorId == UserId ? "sent" : "received", t.Estado,
        new(t.UsuarioCreadorId, t.UsuarioCreador.UserName),
        new(t.UsuarioDestinatarioId, t.UsuarioDestinatario.UserName),
        new(t.PokemonUserR, t.PokemonUserRemitente.IdPokemon, t.PokemonUserRemitente.Nombre),
        t.PokemonUserDestinatario is null ? null : new(t.PokemonUserDestinatario.Id,
            t.PokemonUserDestinatario.IdPokemon, t.PokemonUserDestinatario.Nombre),
        t.Fecha, t.FechaAceptacion, t.FechaConfirmacion);
}
