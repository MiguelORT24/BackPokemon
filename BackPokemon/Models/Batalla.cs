using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPokemon.Models;

[Table("Batallas")]
public class Batalla
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PokemonUserR { get; set; }

    [Required]
    public int IdPokemonRival { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    [Required]
    public int PokemonUserGanador { get; set; }

    public PokemonUser PokemonUserRetador { get; set; } = null!;
    public PokemonUser PokemonUserGanadorNavigation { get; set; } = null!;
}
