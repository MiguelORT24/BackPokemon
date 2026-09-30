using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPokemon.Models;

[Table("PokemonCrianza")]
public class PokemonCrianza
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int IdPokemonUser { get; set; }

    [Required]
    [Range(0, 100)]
    public int Hambre { get; set; } = 100;

    [Required]
    [Range(0, 100)]
    public int Energia { get; set; } = 100;

    [Required]
    [Range(0, 100)]
    public int Higiene { get; set; } = 100;

    [Required]
    public DateTime UltimaActualizacion { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime FechaAdopcion { get; set; } = DateTime.UtcNow;

    [Required]
    public bool Vivo { get; set; } = true;

    public DateTime? FechaMuerte { get; set; }

    [ForeignKey(nameof(IdPokemonUser))]
    public PokemonUser PokemonUser { get; set; } = null!;
}
