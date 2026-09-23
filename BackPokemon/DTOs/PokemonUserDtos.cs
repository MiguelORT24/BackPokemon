using System.ComponentModel.DataAnnotations;

namespace BackPokemon.DTOs;

public class PokemonUserDto
{
    public int Id { get; set; }
    public int IdPokemon { get; set; }
    public string IdUsuario { get; set; } = null!;
}

public class CreatePokemonUserDto
{
    [Required]
    public int IdPokemon { get; set; }

    [Required]
    [MaxLength(450)]
    public string IdUsuario { get; set; } = null!;
}

public class UpdatePokemonUserDto
{
    [Required]
    public int IdPokemon { get; set; }

    [Required]
    [MaxLength(450)]
    public string IdUsuario { get; set; } = null!;
}
