using BackPokemon.Models;

namespace BackPokemon.Services;

public sealed class EfectosEstadoService
{
    private readonly Random _random = new();

    public ResultadoTurnoEstado AplicarEfectoInicioTurno(PokemonEstado pokemon)
    {
        return pokemon.Estado switch
        {
            EstadoPokemon.Sano => new ResultadoTurnoEstado(),
            EstadoPokemon.Paralizado => AplicarParalizado(),
            EstadoPokemon.Quemado => AplicarQuemado(pokemon),
            EstadoPokemon.Envenenado => AplicarEnvenenado(pokemon),
            EstadoPokemon.Dormido => AplicarDormido(pokemon),
            EstadoPokemon.Confundido => AplicarConfundido(),
            EstadoPokemon.Congelado => AplicarCongelado(pokemon),
            _ => new ResultadoTurnoEstado()
        };
    }

    private ResultadoTurnoEstado AplicarParalizado()
    {
        var noPuedeAtacar = _random.NextDouble() < 0.125;

        return new ResultadoTurnoEstado
        {
            PuedeAtacar = !noPuedeAtacar,
            MultiplicadorVelocidad = 0.5,
            Mensaje = noPuedeAtacar
                ? "El Pokémon está paralizado y no puede atacar."
                : "El Pokémon está paralizado, pero puede atacar."
        };
    }

    private static ResultadoTurnoEstado AplicarQuemado(PokemonEstado pokemon)
    {
        return new ResultadoTurnoEstado
        {
            DañoRecibido = CalcularDaño(pokemon.VidaTotal, 16),
            MultiplicadorAtaque = 0.5,
            Mensaje = "El Pokémon perdió vida por estar quemado."
        };
    }

    private static ResultadoTurnoEstado AplicarEnvenenado(PokemonEstado pokemon)
    {
        return new ResultadoTurnoEstado
        {
            DañoRecibido = CalcularDaño(pokemon.VidaTotal, 8),
            Mensaje = "El Pokémon perdió vida por estar envenenado."
        };
    }

    private ResultadoTurnoEstado AplicarDormido(PokemonEstado pokemon)
    {
        if (pokemon.DuracionEstado == 0)
        {
            pokemon.DuracionEstado = _random.Next(1, 4);
        }

        pokemon.TurnosEstado++;

        if (pokemon.TurnosEstado <= pokemon.DuracionEstado)
        {
            return new ResultadoTurnoEstado
            {
                PuedeAtacar = false,
                Mensaje = "El Pokémon está dormido y no puede atacar."
            };
        }

        pokemon.Estado = EstadoPokemon.Sano;
        pokemon.TurnosEstado = 0;
        pokemon.DuracionEstado = 0;

        return new ResultadoTurnoEstado
        {
            EstadoEliminado = true,
            Mensaje = "El Pokémon se despertó."
        };
    }

    private ResultadoTurnoEstado AplicarConfundido()
    {
        var seHiere = _random.NextDouble() < 1.0 / 3.0;

        return new ResultadoTurnoEstado
        {
            PuedeAtacar = !seHiere,
            SeHizoDañoASiMismo = seHiere,
            Mensaje = seHiere
                ? "El Pokémon está confundido y se hirió a sí mismo."
                : "El Pokémon está confundido, pero puede atacar."
        };
    }

    private ResultadoTurnoEstado AplicarCongelado(PokemonEstado pokemon)
    {
        if (_random.NextDouble() < 0.20)
        {
            pokemon.Estado = EstadoPokemon.Sano;
            pokemon.TurnosEstado = 0;

            return new ResultadoTurnoEstado
            {
                EstadoEliminado = true,
                Mensaje = "El Pokémon se descongeló."
            };
        }

        return new ResultadoTurnoEstado
        {
            PuedeAtacar = false,
            Mensaje = "El Pokémon está congelado y no puede atacar."
        };
    }

    private static int CalcularDaño(int vidaTotal, int divisor)
    {
        return vidaTotal <= 0 ? 0 : Math.Max(1, vidaTotal / divisor);
    }
}
