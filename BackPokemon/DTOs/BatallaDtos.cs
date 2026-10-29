using System.ComponentModel.DataAnnotations;
using BackPokemon.Models;

namespace BackPokemon.DTOs;

public class BatallaDto
{
    public int Id { get; set; }
    public string IdUsuario { get; set; } = null!;
    public int PokemonUserR { get; set; }
    public int IdPokemonRival { get; set; }
    public ResultadoBatalla Resultado { get; set; }
    public DateTime Fecha { get; set; }
    public Guid? LlaveIdempotencia { get; set; }
    public ItemDto? Recompensa { get; set; }
    public int? CantidadRecompensa { get; set; }
}

public class CreateBatallaDto
{
    [Range(1, int.MaxValue)]
    public int PokemonUserR { get; set; }
    [Range(1, int.MaxValue)]
    public int IdPokemonRival { get; set; }
    [EnumDataType(typeof(ResultadoBatalla))]
    public ResultadoBatalla? Resultado { get; set; }
    // Legacy fields are honored only for GameAdmin and are never trusted for the timestamp.
    public int? PokemonUserGanador { get; set; }
    public DateTime? Fecha { get; set; }
    public Guid? LlaveIdempotencia { get; set; }
}

public class CreateBatallaConRecompensaDto
{
    [Required]
    public Guid LlaveIdempotencia { get; set; }
    [Range(1, int.MaxValue)]
    public int PokemonUserR { get; set; }
    [Range(1, int.MaxValue)]
    public int IdPokemonRival { get; set; }
    [Range(1, int.MaxValue)]
    public int PokemonUserGanador { get; set; }
}

public class UpdateBatallaDto
{
    [Range(1, int.MaxValue)]
    public int PokemonUserR { get; set; }
    [Range(1, int.MaxValue)]
    public int IdPokemonRival { get; set; }
    [EnumDataType(typeof(ResultadoBatalla))]
    public ResultadoBatalla Resultado { get; set; }
}

public record PokemonBatallaDto(int Id, int IdPokemon, string? Nombre, string? UrlFrontal);
public record BatallaHistorialDto(int Id, DateTime Fecha, ResultadoBatalla Resultado,
    int IdPokemonRival, PokemonBatallaDto Pokemon);
public record RachaBatallaDto(ResultadoBatalla Resultado, int Cantidad);
public record MejorPokemonBatallaDto(int Id, int IdPokemon, string? Nombre, int Victorias);
public record RivalBatallaDto(int IdPokemonRival, int Veces);