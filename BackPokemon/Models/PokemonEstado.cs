using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPokemon.Models;

[Table("PokemonEstado")]
public class PokemonEstado
{
    [Key]
    [ForeignKey(nameof(PokemonUser))]
    public int IdPokemonUser { get; set; }

    [Required]
    public int VidaTotal { get; set; }

    [Required]
    public int VidaActual { get; set; }

    [Required]
    [MaxLength(50)]
    public string Estado { get; set; } = null!;

    [Required]
    public int IdPokeball { get; set; }

    [Required]
    public int AtaqueBase { get; set; }

    [Required]
    public int DefensaBase { get; set; }

    [Required]
    public int VelocidadBase { get; set; }

    [Required]
    [MaxLength(50)]
    public string TipoPrimario { get; set; } = null!;

    [MaxLength(50)]
    public string? TipoSecundario { get; set; }

    [Required]
    [MaxLength(500)]
    public string UrlFrontal { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string UrlTrasera { get; set; } = null!;

    public PokemonUser PokemonUser { get; set; } = null!;
}
