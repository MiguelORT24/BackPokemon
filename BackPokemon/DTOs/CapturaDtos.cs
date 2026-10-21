using System.ComponentModel.DataAnnotations;

namespace BackPokemon.DTOs;

public sealed class IntentarCapturaDto
{
    [Range(1, int.MaxValue)]
    public int PokemonId { get; set; }

    [Range(1, int.MaxValue)]
    public int PokeballItemId { get; set; }
}

public sealed record CapturaDto(
    int Id,
    int PokemonId,
    string PokemonNombre,
    int PokeballItemId,
    bool Exitosa,
    float ProbabilidadAplicada,
    int? PokemonUserId,
    DateTime Fecha);

public sealed record ResultadoCapturaDto(
    int CaptureId,
    bool Captured,
    int RemainingPokeballs,
    CapturaDto Capture,
    PokemonCapturadoDto? Pokemon,
    string? Message);

public sealed record PokemonCapturadoDto(
    int Id,
    int IdPokemon,
    string Nombre,
    int VidaActual,
    int VidaTotal,
    string Estado,
    string TipoPrimario,
    string? TipoSecundario,
    string UrlFrontal,
    string UrlTrasera);
