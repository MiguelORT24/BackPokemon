using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPokemon.Models;

[Table("EfectoItem")]
public class EfectoItem
{
    [Key]
    public int ItemId { get; set; }

    [Required, MaxLength(30)]
    public string Tipo { get; set; } = null!; // ball | berry

    public float? MultiplicadorCaptura { get; set; }

    public int? DuracionSegundos { get; set; }

    public Item Item { get; set; } = null!;
}
