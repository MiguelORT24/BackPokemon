using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPokemon.Models;

[Table("PokemonUser")]
public class PokemonUser
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int IdPokemon { get; set; }

    [MaxLength(100)]
    public string? Nombre { get; set; }

    [Required]
    public string IdUsuario { get; set; } = null!;

    public ApplicationUser Usuario { get; set; } = null!;
    public PokemonEstado? EstadoDetalle { get; set; }
    public PokemonCrianza? Crianza { get; set; }
    public ICollection<Intercambio> IntercambiosComoRemitente { get; set; } = new List<Intercambio>();
    public ICollection<Intercambio> IntercambiosComoDestinatario { get; set; } = new List<Intercambio>();
    public ICollection<Batalla> BatallasComoRetador { get; set; } = new List<Batalla>();
    public ICollection<Batalla> BatallasComoContrincante { get; set; } = new List<Batalla>();
    public ICollection<Batalla> BatallasGanadas { get; set; } = new List<Batalla>();
}
