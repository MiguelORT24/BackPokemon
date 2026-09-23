using System.ComponentModel.DataAnnotations;

namespace BackPokemon.DTOs;

public class IntercambioDto
{
    public int Id { get; set; }
    public int PokemonUserR { get; set; }
    public int PokemonUserD { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = null!;
    public bool UserIdR { get; set; }
    public bool UserIdD { get; set; }
}

public class CreateIntercambioDto
{
    [Required]
    public int PokemonUserR { get; set; }

    [Required]
    public int PokemonUserD { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    [Required]
    [MaxLength(50)]
    public string Estado { get; set; } = null!;

    [Required]
    public bool UserIdR { get; set; }

    [Required]
    public bool UserIdD { get; set; }
}

public class UpdateIntercambioDto
{
    [Required]
    public int PokemonUserR { get; set; }

    [Required]
    public int PokemonUserD { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    [Required]
    [MaxLength(50)]
    public string Estado { get; set; } = null!;

    [Required]
    public bool UserIdR { get; set; }

    [Required]
    public bool UserIdD { get; set; }
}
