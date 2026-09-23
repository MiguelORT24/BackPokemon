using System.ComponentModel.DataAnnotations;

namespace BackPokemon.DTOs;

public class ItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public float Probabilidad { get; set; }
    public string UrlImagen { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
}

public class CreateItemDto
{
    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = null!;

    [Range(0, 1)]
    public float Probabilidad { get; set; }

    [Required]
    [MaxLength(500)]
    public string UrlImagen { get; set; } = null!;

    [Required]
    [MaxLength(1000)]
    public string Descripcion { get; set; } = null!;
}

public class UpdateItemDto
{
    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = null!;

    [Range(0, 1)]
    public float Probabilidad { get; set; }

    [Required]
    [MaxLength(500)]
    public string UrlImagen { get; set; } = null!;

    [Required]
    [MaxLength(1000)]
    public string Descripcion { get; set; } = null!;
}
