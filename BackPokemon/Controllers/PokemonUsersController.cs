using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PokemonUsersController : ControllerBase
{
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
            .Select(pokemonUser => ToDto(pokemonUser))
            .ToListAsync(cancellationToken);

        return Ok(pokemonUsers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PokemonUserDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var pokemonUser = await _context.PokemonUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return pokemonUser is null ? NotFound() : Ok(ToDto(pokemonUser));
    }

    [HttpPost]
    public async Task<ActionResult<PokemonUserDto>> Create(
        CreatePokemonUserDto request,
        CancellationToken cancellationToken)
    {
        if (!await _context.Users.AnyAsync(user => user.Id == request.IdUsuario, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.IdUsuario), "El usuario indicado no existe.");
            return ValidationProblem(ModelState);
        }

        var pokemonUser = new PokemonUser
        {
            IdPokemon = request.IdPokemon,
            IdUsuario = request.IdUsuario
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
        var pokemonUser = await _context.PokemonUsers
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (pokemonUser is null)
        {
            return NotFound();
        }

        if (!await _context.Users.AnyAsync(user => user.Id == request.IdUsuario, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.IdUsuario), "El usuario indicado no existe.");
            return ValidationProblem(ModelState);
        }

        pokemonUser.IdPokemon = request.IdPokemon;
        pokemonUser.IdUsuario = request.IdUsuario;
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var pokemonUser = await _context.PokemonUsers
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (pokemonUser is null)
        {
            return NotFound();
        }

        _context.PokemonUsers.Remove(pokemonUser);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static PokemonUserDto ToDto(PokemonUser pokemonUser) => new()
    {
        Id = pokemonUser.Id,
        IdPokemon = pokemonUser.IdPokemon,
        IdUsuario = pokemonUser.IdUsuario
    };
}
