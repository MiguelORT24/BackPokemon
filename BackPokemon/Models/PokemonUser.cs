using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BackPokemon.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrigenPokemon
{
    Desconocido = 0,
    Inicial = 1,
    Captura = 2,
    Intercambio = 3
}

[Table("PokemonUser")]
public class PokemonUser
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int IdPokemon { get; set; }

    [MaxLength(100)]
    public string? Nombre { get; set; }

    [Required]
    public string IdUsuario { get; set; } = null!;

    public DateTime? FechaObtencion { get; set; }
    public OrigenPokemon Origen { get; set; } = OrigenPokemon.Desconocido;

    public ApplicationUser Usuario { get; set; } = null!;
    public PokemonEstado? EstadoDetalle { get; set; }
    public PokemonCrianza? Crianza { get; set; }
    public ICollection<Intercambio> IntercambiosComoRemitente { get; set; } = new List<Intercambio>();
    public ICollection<Intercambio> IntercambiosComoDestinatario { get; set; } = new List<Intercambio>();
    public ICollection<Batalla> BatallasComoRetador { get; set; } = new List<Batalla>();
    public ICollection<Captura> Capturas { get; set; } = new List<Captura>();
}