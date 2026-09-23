using System.ComponentModel.DataAnnotations;

namespace BackPokemon.DTOs;

public class PokedexDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public int PokemonId { get; set; }
    public DateTime Fecha { get; set; }
}

public class CreatePokedexDto
{
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = null!;

    [Required]
    public int PokemonId { get; set; }
}
