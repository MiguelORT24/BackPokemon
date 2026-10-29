using System.Security.Claims;
using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Controllers;

[Authorize]
[ApiController]
[Route("api/tracker")]
public sealed class TrackerController(ApplicationDbContext context, IConfiguration configuration) : ControllerBase
{
    [HttpGet("usuarios")]
    public async Task<IActionResult> GetUsuarios([FromQuery] string? orden = "nombre", [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 20, [FromQuery] string? search = null, [FromQuery] int minBatallas = 5,
        CancellationToken cancellationToken = default)
    {
        orden = (orden ?? "nombre").Trim().ToLowerInvariant();
        if (orden is not ("winrate" or "victorias" or "pokedex" or "pokemon" or "nombre"))
            return BadRequest("orden debe ser winrate, victorias, pokedex, pokemon o nombre.");
        if (pagina < 1 || tamano is < 1 or > 100 || minBatallas < 1)
            return BadRequest("pagina debe ser >= 1, tamano entre 1 y 100 y minBatallas >= 1.");
        search = search?.Trim();
        if (search?.Length > 100) return BadRequest("search no puede exceder 100 caracteres.");
        var normalizedSearch = string.IsNullOrEmpty(search) ? null : search.ToUpperInvariant();
        var users = context.Users.AsNoTracking();
        if (normalizedSearch is not null)
            users = users.Where(user => user.NormalizedUserName != null && user.NormalizedUserName.Contains(normalizedSearch));

        var projected = users.Select(user => new
        {
            user.Id,
            UserName = user.UserName ?? string.Empty,
            Victorias = context.Batallas.Count(battle => battle.IdUsuario == user.Id
                && battle.Resultado == ResultadoBatalla.Victoria),
            Derrotas = context.Batallas.Count(battle => battle.IdUsuario == user.Id
                && battle.Resultado == ResultadoBatalla.Derrota),
            TotalPokemon = context.PokemonUsers.Count(pokemon => pokemon.IdUsuario == user.Id),
            EspeciesRegistradas = context.Pokedex.Where(entry => entry.UserId == user.Id)
                .Select(entry => entry.PokemonId).Distinct().Count()
        }).Select(row => new
        {
            row.Id, row.UserName, row.Victorias, row.Derrotas, row.TotalPokemon, row.EspeciesRegistradas,
            WinRate = row.Victorias + row.Derrotas == 0 ? (double?)null : (double)row.Victorias / (row.Victorias + row.Derrotas),
            CalificaWinRate = row.Victorias + row.Derrotas >= minBatallas
        });

        var total = await projected.CountAsync(cancellationToken);
        var ordered = orden switch
        {
            "winrate" => projected.OrderByDescending(row => row.CalificaWinRate).ThenByDescending(row => row.WinRate)
                .ThenByDescending(row => row.Victorias).ThenBy(row => row.UserName),
            "victorias" => projected.OrderByDescending(row => row.Victorias).ThenBy(row => row.UserName),
            "pokedex" => projected.OrderByDescending(row => row.EspeciesRegistradas).ThenBy(row => row.UserName),
            "pokemon" => projected.OrderByDescending(row => row.TotalPokemon).ThenBy(row => row.UserName),
            _ => projected.OrderBy(row => row.UserName)
        };
        var rows = await ordered.Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(cancellationToken);
        var items = rows.Select((row, index) => new
        {
            posicion = (pagina - 1) * tamano + index + 1,
            id = row.Id,
            userName = row.UserName,
            victorias = row.Victorias,
            derrotas = row.Derrotas,
            winRate = row.WinRate,
            totalPokemon = row.TotalPokemon,
            especiesRegistradas = row.EspeciesRegistradas
        });
        return Ok(new { total, pagina, tamano, items });
    }

    [HttpGet("usuarios/{userId}/resumen")]
    public async Task<IActionResult> GetResumen(string userId, CancellationToken cancellationToken)
    {
        var user = await context.Users.AsNoTracking().Where(value => value.Id == userId)
            .Select(value => new { value.Id, value.UserName }).SingleOrDefaultAsync(cancellationToken);
        if (user is null) return NotFound();

        var pokemon = context.PokemonUsers.AsNoTracking().Where(value => value.IdUsuario == userId);
        var totalPokemon = await pokemon.CountAsync(cancellationToken);
        var sanos = await pokemon.CountAsync(value => value.EstadoDetalle != null
            && value.EstadoDetalle.VidaActual > 0 && value.EstadoDetalle.Estado == EstadoPokemon.Sano, cancellationToken);
        var afectados = await pokemon.CountAsync(value => value.EstadoDetalle != null
            && value.EstadoDetalle.VidaActual > 0 && value.EstadoDetalle.Estado != EstadoPokemon.Sano, cancellationToken);
        var debilitados = await pokemon.CountAsync(value => value.EstadoDetalle != null
            && value.EstadoDetalle.VidaActual == 0, cancellationToken);
        var enCrianza = await context.PokemonCrianzas.CountAsync(value => value.PokemonUser.IdUsuario == userId
            && value.Vivo, cancellationToken);
        var crianzaMuertos = await context.PokemonCrianzas.CountAsync(value => value.PokemonUser.IdUsuario == userId
            && !value.Vivo, cancellationToken);
        var especies = await context.Pokedex.CountAsync(value => value.UserId == userId, cancellationToken);
        var totalObjetos = await context.ItemUsers.Where(value => value.IdUser == userId)
            .Select(value => (int?)value.Cantidad).SumAsync(cancellationToken) ?? 0;
        var objetosDistintos = await context.ItemUsers.CountAsync(value => value.IdUser == userId, cancellationToken);
        var intercambios = await context.Intercambios.CountAsync(value => value.Estado == "confirmed"
            && (value.UsuarioCreadorId == userId || value.UsuarioDestinatarioId == userId), cancellationToken);
        var battleStats = await context.Batallas.Where(value => value.IdUsuario == userId).GroupBy(_ => 1)
            .Select(group => new
            {
                Victorias = group.Count(value => value.Resultado == ResultadoBatalla.Victoria),
                Derrotas = group.Count(value => value.Resultado == ResultadoBatalla.Derrota),
                Huidas = group.Count(value => value.Resultado == ResultadoBatalla.Huida)
            }).SingleOrDefaultAsync(cancellationToken);
        var wins = battleStats?.Victorias ?? 0;
        var losses = battleStats?.Derrotas ?? 0;
        var decisionCount = wins + losses;

        return Ok(new
        {
            id = user.Id,
            userName = user.UserName,
            pokemon = new { total = totalPokemon, sanos, afectados, debilitados, enCrianza, crianzaMuertos },
            pokedex = new { registrados = especies, total = TotalSpecies },
            batallas = new
            {
                victorias = wins, derrotas = losses, huidas = battleStats?.Huidas ?? 0,
                winRate = decisionCount == 0 ? (double?)null : (double)wins / decisionCount
            },
            objetos = new { total = totalObjetos, distintos = objetosDistintos },
            intercambiosConfirmados = intercambios
        });
    }

    private int TotalSpecies => Math.Max(1, configuration.GetValue("Game:TotalSpecies", 151));
}