using System.ComponentModel.DataAnnotations;

namespace BackPokemon.DTOs;

public sealed class AdoptarTamagotchiDto
{
    [Required]
    public int IdPokemonUser { get; set; }
}

public sealed class ActividadTamagotchiDto
{
    [Required]
    public string Tipo { get; set; } = string.Empty;
}

public sealed class TamagotchiDto
{
    public int Id { get; set; }
    public int IdPokemonUser { get; set; }
    public int IdPokemon { get; set; }
    public int Hambre { get; set; }
    public int Energia { get; set; }
    public int Higiene { get; set; }
    public int VidaActual { get; set; }
    public int VidaTotal { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool Vivo { get; set; }
    public DateTime FechaAdopcion { get; set; }
}
