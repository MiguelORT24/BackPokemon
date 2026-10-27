using System.ComponentModel.DataAnnotations;

namespace BackPokemon.DTOs;

public class BatallaDto
{
    public int Id { get; set; }
    public int PokemonUserR { get; set; }
    public int IdPokemonRival { get; set; }
    public DateTime Fecha { get; set; }
    public int PokemonUserGanador { get; set; }
    public Guid LlaveIdempotencia { get; set; }
    public ItemDto? Recompensa { get; set; }
    public int? CantidadRecompensa { get; set; }
}

public class CreateBatallaDto
{
    [Required]
    public Guid LlaveIdempotencia { get; set; }
    [Required]
    public int PokemonUserR { get; set; }

    [Required]
    public int IdPokemonRival { get; set; }

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
    public int IdPokemonRival { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    [Required]
    public int PokemonUserGanador { get; set; }
}
