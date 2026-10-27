using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPokemon.Models;

[Table("Encuentro")]
public class Encuentro
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PokemonId { get; set; }

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

    [Required]
    public bool JugadorDetectado { get; set; }

    [Required]
    public bool TiroPorLaEspaldaDisponible { get; set; }

    [Required, MaxLength(30)]
    public string CalidadTiroDisponible { get; set; } = "normal";

    public DateTime? ComiendoHasta { get; set; }

    [Required]
    public bool Capturado { get; set; }

    public string? CapturadoPorUsuarioId { get; set; }

    public ApplicationUser? CapturadoPorUsuario { get; set; }
    public ApplicationUser? Usuario { get; set; }

    public ICollection<Captura> Capturas { get; set; } = new List<Captura>();
}
