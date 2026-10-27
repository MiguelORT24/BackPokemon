using Microsoft.AspNetCore.Identity;

namespace BackPokemon.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime? UltimaRecargaPokebolas { get; set; }
    public ICollection<PokemonUser> PokemonUsers { get; set; } = new List<PokemonUser>();
    public ICollection<Pokedex> PokedexEntries { get; set; } = new List<Pokedex>();
    public ICollection<ItemUser> ItemUsers { get; set; } = new List<ItemUser>();
    public ICollection<Captura> Capturas { get; set; } = new List<Captura>();
    public ICollection<RecompensaBatalla> RecompensasBatalla { get; set; } = new List<RecompensaBatalla>();
}
