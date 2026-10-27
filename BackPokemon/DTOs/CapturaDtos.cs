using System.ComponentModel.DataAnnotations;

namespace BackPokemon.DTOs;

public sealed class IntentarCapturaDto
{
    [Range(1, int.MaxValue)]
    public int EncuentroId { get; set; }

    [Range(1, int.MaxValue)]
    public int PokeballItemId { get; set; }

}

public sealed record CapturaDto(
    int Id,
    int? EncuentroId,
    int PokemonId,
    string PokemonNombre,
    int PokeballItemId,
    bool Exitosa,
    float ProbabilidadAplicada,
    int? PokemonUserId,
    DateTime Fecha,
    int CaptureRate,
    float MultiplicadorBola,
    float MultiplicadorSituacional,
    bool NoTeVio,
    bool TiroPorLaEspalda,
    bool PokemonComiendo,
    bool PokemonDormido,
    float ProbabilidadCritica,
    bool FueCritica,
    string CalidadTiro);

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
