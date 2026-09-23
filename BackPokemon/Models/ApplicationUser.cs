using Microsoft.AspNetCore.Identity;

namespace BackPokemon.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<PokemonUser> PokemonUsers { get; set; } = new List<PokemonUser>();
    public ICollection<Pokedex> PokedexEntries { get; set; } = new List<Pokedex>();
    public ICollection<ItemUser> ItemUsers { get; set; } = new List<ItemUser>();
}
