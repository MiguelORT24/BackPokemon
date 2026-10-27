using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPokemon.Models;

[Table("Captura")]
public class Captura
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UsuarioId { get; set; } = null!;

    [Required]
    public int PokemonId { get; set; }

    public int? EncuentroId { get; set; }

    [Required]
    [MaxLength(100)]
    public string PokemonNombre { get; set; } = null!;

    [Required]
    public int IdPokeball { get; set; }

    [Required]
    public bool Exitosa { get; set; }

    [Required]
    public float ProbabilidadAplicada { get; set; }

    public int CaptureRate { get; set; }

    public float MultiplicadorBola { get; set; }

    public float MultiplicadorSituacional { get; set; }

    public bool NoTeVio { get; set; }

    public bool TiroPorLaEspalda { get; set; }

    public bool PokemonComiendo { get; set; }

    public bool PokemonDormido { get; set; }

    public float ProbabilidadCritica { get; set; }

    public bool FueCritica { get; set; }

    [MaxLength(30)]
    public string CalidadTiro { get; set; } = "normal";

    [Required]
    public DateTime Fecha { get; set; }

    public int? IdPokemonUser { get; set; }

    public ApplicationUser Usuario { get; set; } = null!;
    public Item Pokeball { get; set; } = null!;
    public PokemonUser? PokemonUser { get; set; }
    public Encuentro? Encuentro { get; set; }
}
