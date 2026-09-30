using System.ComponentModel.DataAnnotations;
using BackPokemon.Models;

namespace BackPokemon.DTOs;

public sealed class ActualizarPokemonEstadoDto
{
    [Range(0, int.MaxValue)]
    public int VidaActual { get; set; }

    [EnumDataType(typeof(EstadoPokemon))]
    public EstadoPokemon Estado { get; set; }
}
