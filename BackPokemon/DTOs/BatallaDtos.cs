using System.ComponentModel.DataAnnotations;

namespace BackPokemon.DTOs;

public class BatallaDto
{
    public int Id { get; set; }
    public int PokemonUserR { get; set; }
    public int PokemonUserC { get; set; }
    public DateTime Fecha { get; set; }
    public int PokemonUserGanador { get; set; }
}

public class CreateBatallaDto
{
    [Required]
    public int PokemonUserR { get; set; }

    [Required]
    public int PokemonUserC { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    [Required]
    public int PokemonUserGanador { get; set; }
}

public class UpdateBatallaDto
{
    [Required]
    public int PokemonUserR { get; set; }

    [Required]
    public int PokemonUserC { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    [Required]
    public int PokemonUserGanador { get; set; }
}
