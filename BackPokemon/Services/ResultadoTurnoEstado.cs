namespace BackPokemon.Services;

public sealed class ResultadoTurnoEstado
{
    public int DañoRecibido { get; init; }

    public bool PuedeAtacar { get; init; } = true;

    public bool SeHizoDañoASiMismo { get; init; }

    public bool EstadoEliminado { get; init; }

    public double MultiplicadorAtaque { get; init; } = 1.0;

    public double MultiplicadorVelocidad { get; init; } = 1.0;

    public string? Mensaje { get; init; }
}
