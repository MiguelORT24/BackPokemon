using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace BackPokemon.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Route("api/PokemonUser")]
public class PokemonUsersController : ControllerBase
{
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private readonly ApplicationDbContext _context;

    public PokemonUsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PokemonUserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var pokemonUsers = await _context.PokemonUsers
            .AsNoTracking()
            .Where(p => p.IdUsuario == CurrentUserId)
            .Select(pokemonUser => ToDto(pokemonUser))
            .ToListAsync(cancellationToken);

        return Ok(pokemonUsers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PokemonUserDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var pokemonUser = await _context.PokemonUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && item.IdUsuario == CurrentUserId, cancellationToken);

        return pokemonUser is null ? NotFound() : Ok(ToDto(pokemonUser));
    }

    [HttpGet("usuario/{userId}")]
    public async Task<ActionResult<IEnumerable<PokemonUserConEstadoDto>>> GetByUsuario(
        string userId,
        CancellationToken cancellationToken)
    {
        if (userId != CurrentUserId) return NotFound();
        var pokemonUsers = await _context.PokemonUsers
            .AsNoTracking()
            .Where(pokemonUser => pokemonUser.IdUsuario == userId)
            .Select(pokemonUser => new PokemonUserConEstadoDto
            {
                Id = pokemonUser.Id,
                IdPokemon = pokemonUser.IdPokemon,
                IdUsuario = pokemonUser.IdUsuario,
                Nombre = pokemonUser.Nombre,
                PokemonEstado = pokemonUser.EstadoDetalle == null
                    ? null
                    : new PokemonEstadoDto
                    {
                        VidaTotal = pokemonUser.EstadoDetalle.VidaTotal,
                        VidaActual = pokemonUser.EstadoDetalle.VidaActual,
                        Estado = pokemonUser.EstadoDetalle.Estado,
                        AtaqueBase = pokemonUser.EstadoDetalle.AtaqueBase,
                        DefensaBase = pokemonUser.EstadoDetalle.DefensaBase,
                        VelocidadBase = pokemonUser.EstadoDetalle.VelocidadBase,
                        TipoPrimario = pokemonUser.EstadoDetalle.TipoPrimario,
                        TipoSecundario = pokemonUser.EstadoDetalle.TipoSecundario,
                        UrlFrontal = pokemonUser.EstadoDetalle.UrlFrontal,
                        UrlTrasera = pokemonUser.EstadoDetalle.UrlTrasera
                    }
            })
            .ToListAsync(cancellationToken);

        return Ok(pokemonUsers);
    }

    [HttpPost]
    public async Task<ActionResult<PokemonUserDto>> Create(
        CreatePokemonUserDto request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (request.IdUsuario != CurrentUserId) return Forbid();
        if (!await _context.Users.AnyAsync(user => user.Id == request.IdUsuario, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.IdUsuario), "El usuario indicado no existe.");
            return ValidationProblem(ModelState);
        }

        var pokemonUser = new PokemonUser
        {
            IdPokemon = request.IdPokemon,
            IdUsuario = request.IdUsuario,
            Nombre = request.Nombre
        };

        _context.PokemonUsers.Add(pokemonUser);
        await _context.SaveChangesAsync(cancellationToken);

        var response = ToDto(pokemonUser);
        return CreatedAtAction(nameof(GetById), new { id = pokemonUser.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdatePokemonUserDto request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (request.IdUsuario != CurrentUserId) return Forbid();
        var pokemonUser = await _context.PokemonUsers
            .FirstOrDefaultAsync(item => item.Id == id && item.IdUsuario == CurrentUserId, cancellationToken);

        if (pokemonUser is null)
        {
            return NotFound();
        }

        if (!await _context.Users.AnyAsync(user => user.Id == request.IdUsuario, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.IdUsuario), "El usuario indicado no existe.");
            return ValidationProblem(ModelState);
        }

        if (pokemonUser.IdPokemon != request.IdPokemon)
            return Conflict("No se puede cambiar la especie de un Pokémon existente.");
        pokemonUser.Nombre = request.Nombre;
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var pokemonUser = await _context.PokemonUsers
            .FirstOrDefaultAsync(item => item.Id == id && item.IdUsuario == CurrentUserId, cancellationToken);

        if (pokemonUser is null)
        {
            return NotFound();
        }

        if (await _context.Intercambios.AnyAsync(t => t.PokemonUserR == id || t.PokemonUserD == id, cancellationToken))
            return Conflict("El Pokémon tiene historial de intercambios y no se puede eliminar.");
        _context.PokemonUsers.Remove(pokemonUser);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static PokemonUserDto ToDto(PokemonUser pokemonUser) => new()
    {
        Id = pokemonUser.Id,
        IdPokemon = pokemonUser.IdPokemon,
        IdUsuario = pokemonUser.IdUsuario,
        Nombre = pokemonUser.Nombre
    };
}
