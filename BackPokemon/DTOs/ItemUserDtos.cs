using System.ComponentModel.DataAnnotations;

namespace BackPokemon.DTOs;

public class ItemUserDto
{
    public int Id { get; set; }
    public int IdItem { get; set; }
    public string IdUser { get; set; } = null!;
    public int Cantidad { get; set; }
    public ItemDto? Item { get; set; }
}

public class UpdateCantidadItemDto
{
    [Range(0, int.MaxValue)]
    public int Cantidad { get; set; }
}

public class AssignItemDto
{
    [Required]
    public int IdItem { get; set; }

    [Required]
    [MaxLength(450)]
    public string IdUser { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int Cantidad { get; set; }
}

public class UpdateItemUserDto
{
    [Required]
    public int IdItem { get; set; }

    [Required]
    [MaxLength(450)]
    public string IdUser { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int Cantidad { get; set; }
}
