using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BackPokemon.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResultadoBatalla
{
    Victoria = 1,
    Derrota = 2,
    Huida = 3
}

[Table("Batallas")]
public class Batalla
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string IdUsuario { get; set; } = null!;

    [Required]
    public int PokemonUserR { get; set; }

    [Required]
    public int IdPokemonRival { get; set; }

    [Required]
    public ResultadoBatalla Resultado { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    public Guid? LlaveIdempotencia { get; set; }

    public ApplicationUser Usuario { get; set; } = null!;
    public PokemonUser PokemonUserRetador { get; set; } = null!;
    public RecompensaBatalla? Recompensa { get; set; }
}