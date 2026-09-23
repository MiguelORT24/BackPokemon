using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventarioController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public InventarioController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemUserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var inventory = await _context.ItemUsers
            .AsNoTracking()
            .Select(itemUser => ToDto(itemUser))
            .ToListAsync(cancellationToken);

        return Ok(inventory);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemUserDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var itemUser = await _context.ItemUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return itemUser is null ? NotFound() : Ok(ToDto(itemUser));
    }

    [HttpPost]
    public async Task<ActionResult<ItemUserDto>> Create(
        AssignItemDto request,
        CancellationToken cancellationToken)
    {
        if (!await _context.Items.AnyAsync(item => item.Id == request.IdItem, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.IdItem), "El ítem indicado no existe.");
        }

        if (!await _context.Users.AnyAsync(user => user.Id == request.IdUser, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.IdUser), "El usuario indicado no existe.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (await _context.ItemUsers.AnyAsync(
                itemUser => itemUser.IdItem == request.IdItem && itemUser.IdUser == request.IdUser,
                cancellationToken))
        {
            return Conflict("El ítem ya está asignado a este usuario.");
        }

        var itemUser = new ItemUser
        {
            IdItem = request.IdItem,
            IdUser = request.IdUser,
            Cantidad = request.Cantidad
        };

        _context.ItemUsers.Add(itemUser);
        await _context.SaveChangesAsync(cancellationToken);

        var response = ToDto(itemUser);
        return CreatedAtAction(nameof(GetById), new { id = itemUser.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateItemUserDto request,
        CancellationToken cancellationToken)
    {
        var itemUser = await _context.ItemUsers
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (itemUser is null)
        {
            return NotFound();
        }

        if (!await _context.Items.AnyAsync(item => item.Id == request.IdItem, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.IdItem), "El ítem indicado no existe.");
        }

        if (!await _context.Users.AnyAsync(user => user.Id == request.IdUser, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.IdUser), "El usuario indicado no existe.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (await _context.ItemUsers.AnyAsync(
                item => item.Id != id && item.IdItem == request.IdItem && item.IdUser == request.IdUser,
                cancellationToken))
        {
            return Conflict("El ítem ya está asignado a este usuario.");
        }

        itemUser.IdItem = request.IdItem;
        itemUser.IdUser = request.IdUser;
        itemUser.Cantidad = request.Cantidad;
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var itemUser = await _context.ItemUsers
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (itemUser is null)
        {
            return NotFound();
        }

        _context.ItemUsers.Remove(itemUser);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static ItemUserDto ToDto(ItemUser itemUser) => new()
    {
        Id = itemUser.Id,
        IdItem = itemUser.IdItem,
        IdUser = itemUser.IdUser,
        Cantidad = itemUser.Cantidad
    };
}
