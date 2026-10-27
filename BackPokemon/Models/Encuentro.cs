using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPokemon.Models;

[Table("Encuentro")]
public class Encuentro
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string ZonaKey { get; set; } = null!;

    [Required]
    public int PokemonId { get; set; }

    // Retained only for encounters created by the previous per-user implementation.
    // Newly generated shared encounters leave this null.
    [MaxLength(450)]
    public string? UsuarioId { get; set; }

    [Required, MaxLength(100)]
    public string PokemonNombre { get; set; } = null!;

    [Required]
    public double Latitud { get; set; }

    [Required]
    public double Longitud { get; set; }

    [Required]
    public DateTime FechaExpiracion { get; set; }

    [Required]
    public bool Dormido { get; set; }

    public ApplicationUser? Usuario { get; set; }
    public ICollection<EncuentroUsuario> EstadosUsuario { get; set; } = new List<EncuentroUsuario>();
    public ICollection<Captura> Capturas { get; set; } = new List<Captura>();
}
