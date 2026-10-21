using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BackPokemon.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Route("api/ItemUser")]
public class InventarioController : ControllerBase
{
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
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
            .Include(itemUser => itemUser.Item)
            .Where(itemUser => itemUser.IdUser == CurrentUserId)
            .Select(itemUser => ToDto(itemUser))
            .ToListAsync(cancellationToken);

        return Ok(inventory);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemUserDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var itemUser = await _context.ItemUsers
            .Include(item => item.Item)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && item.IdUser == CurrentUserId, cancellationToken);

        return itemUser is null ? NotFound() : Ok(ToDto(itemUser));
    }

    [HttpGet("usuario/{userId}")]
    public async Task<ActionResult<IEnumerable<ItemUserDto>>> GetByUsuario(
        string userId,
        CancellationToken cancellationToken)
    {
        if (userId != CurrentUserId) return NotFound();
        var inventory = await _context.ItemUsers
            .AsNoTracking()
            .Include(itemUser => itemUser.Item)
            .Where(itemUser => itemUser.IdUser == userId)
            .Select(itemUser => ToDto(itemUser))
            .ToListAsync(cancellationToken);

        return Ok(inventory);
    }

    [HttpGet("/api/me/inventory")]
    public Task<ActionResult<IEnumerable<ItemUserDto>>> GetMine(CancellationToken cancellationToken) =>
        GetAll(cancellationToken);

    private static ItemUserDto ToDto(ItemUser itemUser) => new()
    {
        Id = itemUser.Id,
        IdItem = itemUser.IdItem,
        IdUser = itemUser.IdUser,
        Cantidad = itemUser.Cantidad,
        Item = itemUser.Item is null ? null : new ItemDto
        {
            Id = itemUser.Item.Id,
            Nombre = itemUser.Item.Nombre,
            Probabilidad = itemUser.Item.Probabilidad,
            UrlImagen = itemUser.Item.UrlImagen,
            Descripcion = itemUser.Item.Descripcion,
            Efecto = itemUser.Item.Efecto,
            Valor = itemUser.Item.Valor
        }
    };
}
