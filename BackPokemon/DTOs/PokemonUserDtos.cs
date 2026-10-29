using System.ComponentModel.DataAnnotations;
using BackPokemon.Models;

namespace BackPokemon.DTOs;

public class PokemonUserDto
{
    public int Id { get; set; }
    public int IdPokemon { get; set; }
    public string IdUsuario { get; set; } = null!;
    public string? Nombre { get; set; }
    public DateTime? FechaObtencion { get; set; }
    public OrigenPokemon Origen { get; set; }
}

public class PokemonUserConEstadoDto
{
    public int Id { get; set; }
    public int IdPokemon { get; set; }
    public string IdUsuario { get; set; } = null!;
    public string? Nombre { get; set; }
    public DateTime? FechaObtencion { get; set; }
    public OrigenPokemon Origen { get; set; }
    public PokemonEstadoDto? PokemonEstado { get; set; }
    public PokemonCrianzaResumenDto? Crianza { get; set; }
}

public class PokemonCrianzaResumenDto
{
    public int Hambre { get; set; }
    public int Energia { get; set; }
    public int Higiene { get; set; }
    public bool Vivo { get; set; }
    public DateTime FechaAdopcion { get; set; }
    public DateTime? FechaMuerte { get; set; }
}

public class PokemonEstadoDto
{
    public int VidaTotal { get; set; }
    public int VidaActual { get; set; }
    public EstadoPokemon Estado { get; set; }
    public string EstadoNombre { get; set; } = string.Empty;
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