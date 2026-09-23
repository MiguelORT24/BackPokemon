using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPokemon.Models;

[Table("Pokedex")]
public class Pokedex
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    public int PokemonId { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    public ApplicationUser Usuario { get; set; } = null!;
}
