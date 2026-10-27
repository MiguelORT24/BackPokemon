using System.ComponentModel.DataAnnotations;

namespace BackPokemon.DTOs;

public class ItemDto
{
    public int Id { get; set; }
    public int? PokeApiItemId { get; set; }
    public string Nombre { get; set; } = null!;
    public float Probabilidad { get; set; }
    public string UrlImagen { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public string Efecto { get; set; } = null!;
    public int Valor { get; set; }
    public string? TipoJuego { get; set; }
    public float? MultiplicadorCaptura { get; set; }
    public int? DuracionSegundos { get; set; }
}

public sealed class ConfigurarEfectoItemDto
{
    [Required, RegularExpression("^(ball|berry)$")]
    public string Tipo { get; set; } = null!;

    [Range(0.01, 10)]
    public float? MultiplicadorCaptura { get; set; }

    [Range(1, 86400)]
    public int? DuracionSegundos { get; set; }
}

public sealed record SincronizacionItemsDto(int Added, int Updated, int Total);

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

    [Required]
    [MaxLength(100)]
    public string Efecto { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int Valor { get; set; }
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

    [Required]
    [MaxLength(100)]
    public string Efecto { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int Valor { get; set; }
}
