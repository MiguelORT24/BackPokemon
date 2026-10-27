using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPokemon.Models;

[Table("RecompensaBatalla")]
public class RecompensaBatalla
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int BatallaId { get; set; }

    [Required, MaxLength(450)]
    public string UsuarioId { get; set; } = null!;

    [Required]
    public int ItemId { get; set; }

    [Required]
    public int Cantidad { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    public Batalla Batalla { get; set; } = null!;
    public ApplicationUser Usuario { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
