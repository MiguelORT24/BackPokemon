using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPokemon.Models;

[Table("ObjetoCampo")]
public class ObjetoCampo
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ItemId { get; set; }

    [Required]
    public double Latitud { get; set; }

    [Required]
    public double Longitud { get; set; }

    [Required]
    public int Cantidad { get; set; }

    [Required]
    public DateTime FechaExpiracion { get; set; }

    public string? RecogidoPorUsuarioId { get; set; }

    public DateTime? FechaRecogida { get; set; }

    public Item Item { get; set; } = null!;
}
