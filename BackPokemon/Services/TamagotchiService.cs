using BackPokemon.Models;

namespace BackPokemon.Services;

public sealed class TamagotchiService
{
    private const int MaxNecesidad = 100;
    private const int LimiteTamagotchis = 4;

    public int Limite => LimiteTamagotchis;

    public void ActualizarEstado(PokemonCrianza crianza)
    {
        if (!crianza.Vivo)
        {
            return;
        }

        var ahora = DateTime.UtcNow;
        var horasTranscurridas = (ahora - crianza.UltimaActualizacion).TotalHours;

        if (horasTranscurridas <= 0)
        {
            return;
        }

        var perdida = (int)Math.Floor(horasTranscurridas * MaxNecesidad / 24);

        if (perdida <= 0)
        {
            return;
        }

        var hambreAnterior = crianza.Hambre;
        var energiaAnterior = crianza.Energia;
        var higieneAnterior = crianza.Higiene;

        crianza.Hambre = Math.Max(0, crianza.Hambre - perdida);
        crianza.Energia = Math.Max(0, crianza.Energia - perdida);
        crianza.Higiene = Math.Max(0, crianza.Higiene - perdida);

        var daño = Math.Max(0, perdida - hambreAnterior)
            + Math.Max(0, perdida - energiaAnterior)
            + Math.Max(0, perdida - higieneAnterior);

        if (daño > 0 && crianza.PokemonUser.EstadoDetalle is not null)
        {
            var estado = crianza.PokemonUser.EstadoDetalle;
            estado.VidaActual = Math.Max(0, estado.VidaActual - daño);

            if (estado.VidaActual == 0)
            {
                crianza.Vivo = false;
                crianza.FechaMuerte = ahora;
            }
        }

        crianza.UltimaActualizacion = ahora;
    }

    public void AplicarActividad(PokemonCrianza crianza, string tipo)
    {
        if (!crianza.Vivo)
        {
            throw new InvalidOperationException("El Tamagotchi no está vivo.");
        }

        var estado = crianza.PokemonUser.EstadoDetalle
            ?? throw new InvalidOperationException("El Pokémon no tiene estado registrado.");

        switch (tipo.Trim().ToLowerInvariant())
        {
            case "comer":
                crianza.Hambre = Math.Min(MaxNecesidad, crianza.Hambre + 25);
                estado.Estado = EstadoPokemon.Sano;
                break;

            case "dormir":
                crianza.Energia = Math.Min(MaxNecesidad, crianza.Energia + 30);
                estado.Estado = EstadoPokemon.Dormido;
                break;

            case "banar":
            case "bañar":
                crianza.Higiene = Math.Min(MaxNecesidad, crianza.Higiene + 30);
                estado.Estado = EstadoPokemon.Sano;
                break;

            case "curar":
                estado.VidaActual = Math.Min(
                    estado.VidaTotal,
                    estado.VidaActual + 20);
                estado.Estado = EstadoPokemon.Sano;
                break;

            default:
                throw new ArgumentException(
                    "La actividad debe ser comer, dormir, bañar o curar.",
                    nameof(tipo));
        }

        crianza.UltimaActualizacion = DateTime.UtcNow;
    }
}
