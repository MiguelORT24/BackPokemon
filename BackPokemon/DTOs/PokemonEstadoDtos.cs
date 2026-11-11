using System.ComponentModel.DataAnnotations;
using BackPokemon.Models;

namespace BackPokemon.DTOs;

public sealed class ActualizarPokemonEstadoDto
{
    [Range(0, int.MaxValue)]
    public int VidaActual { get; set; }

    [EnumDataType(typeof(EstadoPokemon))]
    public EstadoPokemon Estado { get; set; }
}

public sealed class CrearPokemonEstadoDto
{
    [Range(1, int.MaxValue)]
    public int IdPokemonUser { get; set; }

    [Range(1, int.MaxValue)]
    public int VidaTotal { get; set; }

    [Range(0, int.MaxValue)]
    public int VidaActual { get; set; }

    [EnumDataType(typeof(EstadoPokemon))]
    public EstadoPokemon Estado { get; set; } = EstadoPokemon.Sano;

    [Range(1, int.MaxValue)]
    public int IdPokeball { get; set; }

    [Range(0, int.MaxValue)]
    public int AtaqueBase { get; set; }

    [Range(0, int.MaxValue)]
    public int DefensaBase { get; set; }

    [Range(0, int.MaxValue)]
    public int VelocidadBase { get; set; }

    [Required, StringLength(50, MinimumLength = 1)]
    public string TipoPrimario { get; set; } = null!;

    [StringLength(50)]
    public string? TipoSecundario { get; set; }

    [Required, StringLength(500, MinimumLength = 1)]
    public string UrlFrontal { get; set; } = null!;

    [Required, StringLength(500, MinimumLength = 1)]
    public string UrlTrasera { get; set; } = null!;
}
