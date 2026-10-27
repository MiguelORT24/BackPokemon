using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BackPokemon.Services;

namespace BackPokemon.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/Item")]
public class ItemsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly PokeApiItemSyncService _itemSyncService;

    public ItemsController(ApplicationDbContext context, PokeApiItemSyncService itemSyncService)
    {
        _context = context;
        _itemSyncService = itemSyncService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _context.Items
            .AsNoTracking()
            .Include(item => item.EfectoJuego)
            .Select(item => ToDto(item))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _context.Items
            .AsNoTracking()
            .Include(value => value.EfectoJuego)
            .FirstOrDefaultAsync(value => value.Id == id, cancellationToken);

        return item is null ? NotFound() : Ok(ToDto(item));
    }

    [Authorize(Roles = "GameAdmin")]
    [HttpPut("{id:int}/regla-juego")]
    public async Task<IActionResult> ConfigureGameEffect(int id, ConfigurarEfectoItemDto request,
        CancellationToken cancellationToken)
    {
        if (request.Tipo == "ball" && request.MultiplicadorCaptura is null)
            return BadRequest("Una Poké Ball requiere MultiplicadorCaptura.");
        if (request.Tipo == "berry" && request.DuracionSegundos is null)
            return BadRequest("Una baya requiere DuracionSegundos.");
        var item = await _context.Items.Include(value => value.EfectoJuego)
            .FirstOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (item is null) return NotFound();
        var effect = item.EfectoJuego ?? new EfectoItem { Item = item };
        effect.Tipo = request.Tipo;
        effect.MultiplicadorCaptura = request.Tipo == "ball" ? request.MultiplicadorCaptura : null;
        effect.DuracionSegundos = request.Tipo == "berry" ? request.DuracionSegundos : null;
        if (item.EfectoJuego is null) _context.EfectosItems.Add(effect);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "GameAdmin")]
    [HttpPost("sync/capture-items")]
    [HttpPost("sync/poke-balls")]
    public async Task<IActionResult> SyncCaptureItems(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _itemSyncService.SyncCaptureItems(cancellationToken));
        }
        catch (PokeApiUnavailableException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "No se pudo sincronizar el catálogo desde PokéAPI.");
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "No se pudo conectar con PokéAPI.");
        }
        catch (TaskCanceledException)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, "La sincronización agotó el tiempo de espera.");
        }
    }

    [HttpPost]
    [Authorize(Roles = "GameAdmin")]
    public async Task<ActionResult<ItemDto>> Create(
        CreateItemDto request,
        CancellationToken cancellationToken)
    {
        var item = new Item
        {
            Nombre = request.Nombre,
            Probabilidad = request.Probabilidad,
            UrlImagen = request.UrlImagen,
            Descripcion = request.Descripcion,
            Efecto = request.Efecto,
            Valor = request.Valor
        };

        _context.Items.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        var response = ToDto(item);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "GameAdmin")]
    public async Task<IActionResult> Update(
        int id,
        UpdateItemDto request,
        CancellationToken cancellationToken)
    {
        var item = await _context.Items
            .FirstOrDefaultAsync(value => value.Id == id, cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        item.Nombre = request.Nombre;
        item.Probabilidad = request.Probabilidad;
        item.UrlImagen = request.UrlImagen;
        item.Descripcion = request.Descripcion;
        item.Efecto = request.Efecto;
        item.Valor = request.Valor;
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "GameAdmin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var item = await _context.Items
            .FirstOrDefaultAsync(value => value.Id == id, cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        if (await _context.ItemUsers.AnyAsync(itemUser => itemUser.IdItem == id, cancellationToken)
            || await _context.Capturas.AnyAsync(capture => capture.IdPokeball == id, cancellationToken)
            || await _context.RecompensasBatalla.AnyAsync(reward => reward.ItemId == id, cancellationToken)
            || await _context.ObjetosCampo.AnyAsync(drop => drop.ItemId == id, cancellationToken))
        {
            return Conflict("No se puede eliminar el ítem porque forma parte de inventarios o historial del juego.");
        }

        _context.Items.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static ItemDto ToDto(Item item) => new()
    {
        Id = item.Id,
        PokeApiItemId = item.PokeApiItemId,
        Nombre = item.Nombre,
        Probabilidad = item.Probabilidad,
        UrlImagen = item.UrlImagen,
        Descripcion = item.Descripcion,
        Efecto = item.Efecto,
        Valor = item.Valor,
        TipoJuego = item.EfectoJuego?.Tipo,
        MultiplicadorCaptura = item.EfectoJuego?.MultiplicadorCaptura,
        DuracionSegundos = item.EfectoJuego?.DuracionSegundos
    };
}
