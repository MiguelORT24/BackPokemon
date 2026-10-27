using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPokemon.Models;

[Table("EncuentroUsuario")]
public sealed class EncuentroUsuario
{
    public int EncuentroId { get; set; }

    [Required, MaxLength(450)]
    public string UsuarioId { get; set; } = null!;

    [Required]
    public bool JugadorDetectado { get; set; }

    [Required]
    public bool TiroPorLaEspaldaDisponible { get; set; }

    [Required, MaxLength(30)]
    public string CalidadTiroDisponible { get; set; } = "normal";

    public DateTime? ComiendoHasta { get; set; }

    [Required]
    public bool Capturado { get; set; }

    public DateTime? FechaCaptura { get; set; }

    public Encuentro Encuentro { get; set; } = null!;
    public ApplicationUser Usuario { get; set; } = null!;
}
