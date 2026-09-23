using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPokemon.Models;

[Table("Intercambio")]
public class Intercambio
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PokemonUserR { get; set; }

    [Required]
    public int PokemonUserD { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    [Required]
    [MaxLength(50)]
    public string Estado { get; set; } = null!;

    [Required]
    public bool UserIdR { get; set; }

    [Required]
    public bool UserIdD { get; set; }

    public PokemonUser PokemonUserRemitente { get; set; } = null!;
    public PokemonUser PokemonUserDestinatario { get; set; } = null!;
}
