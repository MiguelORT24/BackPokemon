using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPokemon.Models;

[Table("ItemUser")]
public class ItemUser
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int IdItem { get; set; }

    [Required]
    public string IdUser { get; set; } = null!;

    [Required]
    public int Cantidad { get; set; }

    public Item Item { get; set; } = null!;
    public ApplicationUser Usuario { get; set; } = null!;
}
