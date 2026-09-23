using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ItemsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _context.Items
            .AsNoTracking()
            .Select(item => ToDto(item))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _context.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(value => value.Id == id, cancellationToken);

        return item is null ? NotFound() : Ok(ToDto(item));
    }

    [HttpPost]
    public async Task<ActionResult<ItemDto>> Create(
        CreateItemDto request,
        CancellationToken cancellationToken)
    {
        var item = new Item
        {
            Nombre = request.Nombre,
            Probabilidad = request.Probabilidad,
            UrlImagen = request.UrlImagen,
            Descripcion = request.Descripcion
        };

        _context.Items.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        var response = ToDto(item);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, response);
    }

    [HttpPut("{id:int}")]
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
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var item = await _context.Items
            .FirstOrDefaultAsync(value => value.Id == id, cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        if (await _context.ItemUsers.AnyAsync(itemUser => itemUser.IdItem == id, cancellationToken))
        {
            return Conflict("No se puede eliminar el ítem porque está asignado a usuarios.");
        }

        _context.Items.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static ItemDto ToDto(Item item) => new()
    {
        Id = item.Id,
        Nombre = item.Nombre,
        Probabilidad = item.Probabilidad,
        UrlImagen = item.UrlImagen,
        Descripcion = item.Descripcion
    };
}
