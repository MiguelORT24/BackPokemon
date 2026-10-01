using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BackPokemon.Models;
[Table("Intercambio")]
public class Intercambio
{
    public int Id { get; set; }
    public int PokemonUserR { get; set; }
    public int? PokemonUserD { get; set; }
    public DateTime Fecha { get; set; }
    public DateTime? FechaAceptacion { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    [MaxLength(50)] public string Estado { get; set; } = "pending";
    public string UsuarioCreadorId { get; set; } = null!;
    public string UsuarioDestinatarioId { get; set; } = null!;
    public ApplicationUser UsuarioCreador { get; set; } = null!;
    public ApplicationUser UsuarioDestinatario { get; set; } = null!;
    public PokemonUser PokemonUserRemitente { get; set; } = null!;
    public PokemonUser? PokemonUserDestinatario { get; set; }
}
