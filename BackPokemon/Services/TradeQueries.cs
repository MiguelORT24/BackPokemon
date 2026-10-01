using BackPokemon.Data;
using BackPokemon.Models;
namespace BackPokemon.Services;
public static class TradeQueries
{
    public static IQueryable<PokemonUser> AvailablePokemon(this ApplicationDbContext db, int? exceptTrade = null)
        => db.PokemonUsers.Where(p => p.EstadoDetalle != null && p.EstadoDetalle.VidaActual > 0
            && (p.Crianza == null || p.Crianza.Vivo)
            && !db.Intercambios.Any(t => (!exceptTrade.HasValue || t.Id != exceptTrade.Value)
                && (t.Estado == "pending" || t.Estado == "accepted")
                && (t.PokemonUserR == p.Id || t.PokemonUserD == p.Id)));
}
