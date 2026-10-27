using System.Data;
using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Controllers;

[Authorize]
[ApiController]
[Route("api/objetos-campo")]
public sealed class ObjetosCampoController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("cercanos")]
    public async Task<IActionResult> GetNearby([FromQuery] double latitud, [FromQuery] double longitud,
        [FromQuery] double radioMetros = 500, CancellationToken cancellationToken = default)
    {
        if (latitud is < -90 or > 90 || longitud is < -180 or > 180 || radioMetros is <= 0 or > 5000)
            return BadRequest("Coordenadas o radio fuera de rango.");
        var now = DateTime.UtcNow;
        var drops = await context.ObjetosCampo.AsNoTracking().Include(drop => drop.Item)
            .Where(drop => drop.RecogidoPorUsuarioId == null && drop.FechaExpiracion > now)
            .ToListAsync(cancellationToken);
        return Ok(drops.Where(drop => EncuentrosController.DistanceMeters(latitud, longitud,
                drop.Latitud, drop.Longitud) <= radioMetros)
            .Select(drop => new ObjetoCampoDto(drop.Id, drop.ItemId, drop.Item.Nombre,
                drop.Item.UrlImagen, drop.Cantidad, drop.Latitud, drop.Longitud, drop.FechaExpiracion)));
    }

    [Authorize(Roles = "GameAdmin")]
    [HttpPost]
    public async Task<IActionResult> Spawn(CrearObjetoCampoDto request, CancellationToken cancellationToken)
    {
        var validItem = await context.EfectosItems.AnyAsync(effect => effect.ItemId == request.ItemId
            && (effect.Tipo == "ball" || effect.Tipo == "berry"), cancellationToken);
        if (!validItem) return BadRequest("Solo se pueden colocar en el campo objetos de captura.");
        var drop = new ObjetoCampo
        {
            ItemId = request.ItemId, Latitud = request.Latitud, Longitud = request.Longitud,
            Cantidad = request.Cantidad, FechaExpiracion = DateTime.UtcNow.AddMinutes(request.DuracionMinutos)
        };
        context.ObjetosCampo.Add(drop);
        await context.SaveChangesAsync(cancellationToken);
        var item = await context.Items.AsNoTracking().FirstAsync(value => value.Id == request.ItemId, cancellationToken);
        return Ok(new ObjetoCampoDto(drop.Id, drop.ItemId, item.Nombre, item.UrlImagen,
            drop.Cantidad, drop.Latitud, drop.Longitud, drop.FechaExpiracion));
    }

    [HttpPost("{id:int}/recoger")]
    public async Task<IActionResult> Collect(int id, [FromQuery] double latitud, [FromQuery] double longitud,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();
        if (latitud is < -90 or > 90 || longitud is < -180 or > 180) return BadRequest("Coordenadas fuera de rango.");
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        var drop = await context.ObjetosCampo.Include(value => value.Item)
            .FirstOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (drop is null) return NotFound();
        if (drop.RecogidoPorUsuarioId is not null || drop.FechaExpiracion <= DateTime.UtcNow)
            return Conflict("El objeto ya fue recogido o expiró.");
        if (EncuentrosController.DistanceMeters(latitud, longitud, drop.Latitud, drop.Longitud) > 50)
            return BadRequest("Debes estar a menos de 50 metros del objeto para recogerlo.");

        var inventory = await context.ItemUsers.FirstOrDefaultAsync(value => value.IdUser == userId
            && value.IdItem == drop.ItemId, cancellationToken);
        if (inventory is null)
        {
            inventory = new ItemUser { IdUser = userId, IdItem = drop.ItemId, Cantidad = 0 };
            context.ItemUsers.Add(inventory);
        }
        inventory.Cantidad += drop.Cantidad;
        drop.RecogidoPorUsuarioId = userId;
        drop.FechaRecogida = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { drop.Id, drop.ItemId, drop.Item.Nombre, inventory.Cantidad });
    }
}
