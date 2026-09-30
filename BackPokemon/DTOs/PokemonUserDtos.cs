using System.ComponentModel.DataAnnotations;
using BackPokemon.Models;

namespace BackPokemon.DTOs;

public class PokemonUserDto
{
    public int Id { get; set; }
    public int IdPokemon { get; set; }
    public string IdUsuario { get; set; } = null!;
    public string? Nombre { get; set; }
}

public class PokemonUserConEstadoDto
{
    public int Id { get; set; }
    public int IdPokemon { get; set; }
    public string IdUsuario { get; set; } = null!;
    public string? Nombre { get; set; }
    public PokemonEstadoDto? PokemonEstado { get; set; }
}

public class PokemonEstadoDto
{
    public int VidaTotal { get; set; }
    public int VidaActual { get; set; }
    public EstadoPokemon Estado { get; set; }
    public int AtaqueBase { get; set; }
    public int DefensaBase { get; set; }
    public int VelocidadBase { get; set; }
    public string TipoPrimario { get; set; } = null!;
    public string? TipoSecundario { get; set; }
    public string UrlFrontal { get; set; } = null!;
    public string UrlTrasera { get; set; } = null!;
}

public class CreatePokemonUserDto
{
    [Required]
    public int IdPokemon { get; set; }

    [MaxLength(100)]
    public string? Nombre { get; set; }

    [Required]
    [MaxLength(450)]
    public string IdUsuario { get; set; } = null!;
}

public class UpdatePokemonUserDto
{
    [Required]
    public int IdPokemon { get; set; }

    [MaxLength(100)]
    public string? Nombre { get; set; }

    [Required]
    [MaxLength(450)]
    public string IdUsuario { get; set; } = null!;
}
