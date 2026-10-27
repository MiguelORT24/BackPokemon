using System.ComponentModel.DataAnnotations;

namespace BackPokemon.DTOs;

public sealed class CrearEncuentroDto
{
    [Range(1, int.MaxValue)] public int PokemonId { get; set; }
    [Range(-90, 90)] public double Latitud { get; set; }
    [Range(-180, 180)] public double Longitud { get; set; }
    public bool Dormido { get; set; }
}

public sealed class ActualizarEstadoEncuentroDto
{
    public bool Dormido { get; set; }
    [MaxLength(450)] public string? UsuarioId { get; set; }
    public bool JugadorDetectado { get; set; }
    public bool TiroPorLaEspaldaDisponible { get; set; }
    [RegularExpression("^(normal|bueno|excelente|perfecto)$")]
    public string CalidadTiroDisponible { get; set; } = "normal";
}

public sealed record EncuentroDto(
    int Id, int PokemonId, string PokemonNombre, double Latitud, double Longitud,
    DateTime FechaExpiracion, bool Dormido, bool Comiendo, bool JugadorDetectado,
    bool TiroPorLaEspaldaDisponible);

public sealed class UsarBayaDto
{
    [Range(1, int.MaxValue)] public int BayaItemId { get; set; }
}

public sealed class CrearObjetoCampoDto
{
    [Range(1, int.MaxValue)] public int ItemId { get; set; }
    [Range(-90, 90)] public double Latitud { get; set; }
    [Range(-180, 180)] public double Longitud { get; set; }
    [Range(1, 100)] public int Cantidad { get; set; } = 1;
    [Range(1, 1440)] public int DuracionMinutos { get; set; } = 60;
}

public sealed record ObjetoCampoDto(int Id, int ItemId, string Nombre, string? UrlImagen,
    int Cantidad, double Latitud, double Longitud, DateTime FechaExpiracion);
