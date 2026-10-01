using System.Security.Claims;
using BackPokemon.Data;
using BackPokemon.Models;
using BackPokemon.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
[ApiController]
[Route("api/me")]
[Route("me")]
public class UsuarioController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("pokemon")]
    public async Task<IActionResult> GetMyPokemon([FromQuery(Name = "health_lt")] int? healthLt,
        [FromQuery] bool? available, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();
        if (healthLt is < 0 or > 100) return BadRequest("health_lt debe estar entre 0 y 100.");
        var query = context.PokemonUsers.AsNoTracking().Where(p => p.IdUsuario == userId);
        var availableIds = context.AvailablePokemon().Select(p => p.Id);
        if (healthLt.HasValue)
            query = query.Where(p => p.EstadoDetalle != null && p.EstadoDetalle.VidaActual < healthLt.Value);
        if (available.HasValue)
            query = query.Where(p => availableIds.Contains(p.Id) == available.Value);
        return Ok(await query.Select(p => new
        {
            p.Id, p.IdPokemon, p.IdUsuario, p.Nombre,
            VidaActual = p.EstadoDetalle == null ? (int?)null : p.EstadoDetalle.VidaActual,
            VidaTotal = p.EstadoDetalle == null ? (int?)null : p.EstadoDetalle.VidaTotal,
            Estado = p.EstadoDetalle == null ? null : p.EstadoDetalle.Estado.ToString(),
            Available = availableIds.Contains(p.Id),
            Reserved = context.Intercambios.Any(t => (t.Estado == "pending" || t.Estado == "accepted")
                && (t.PokemonUserR == p.Id || t.PokemonUserD == p.Id)),
            Crianza = p.Crianza == null ? null : new
            {
                p.Crianza.Hambre, p.Crianza.Energia, p.Crianza.Higiene, p.Crianza.Vivo,
                p.Crianza.UltimaActualizacion, p.Crianza.FechaAdopcion, p.Crianza.FechaMuerte
            }
        }).ToListAsync(cancellationToken));
    }

    [HttpPost("pokemon/heal")]
    public async Task<IActionResult> Heal(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();
        // Curar no revive mascotas que ya murieron en Tamagotchi.
        var healed = await context.PokemonEstados.Where(e => e.PokemonUser.IdUsuario == userId
            && (e.PokemonUser.Crianza == null || e.PokemonUser.Crianza.Vivo))
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.VidaActual, e => e.VidaTotal)
                .SetProperty(e => e.Estado, EstadoPokemon.Sano)
                .SetProperty(e => e.TurnosEstado, 0).SetProperty(e => e.DuracionEstado, 0), cancellationToken);
        return Ok(new { healed });
    }
}
