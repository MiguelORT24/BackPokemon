using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPokemon.Models;

[Table("Item")]
public class Item
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = null!;

    [Required]
    public float Probabilidad { get; set; }

    [Required]
    [MaxLength(500)]
    public string UrlImagen { get; set; } = null!;

    [Required]
    [MaxLength(1000)]
    public string Descripcion { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Efecto { get; set; } = string.Empty;

    [Required]
    public int Valor { get; set; }

    public ICollection<ItemUser> ItemUsers { get; set; } = new List<ItemUser>();
}
