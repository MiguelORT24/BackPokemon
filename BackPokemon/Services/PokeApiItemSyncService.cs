using System.Data;
using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Services;

public sealed class PokeApiItemSyncService(ApplicationDbContext context, PokeApiService pokeApi)
{
    public async Task<SincronizacionItemsDto> SyncPokeBalls(CancellationToken cancellationToken)
    {
        var remoteItems = await pokeApi.GetPokeBallItems(cancellationToken);
        var remoteIds = remoteItems.Select(item => item.Id).ToArray();

        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var localItems = await context.Items
            .Where(item => item.PokeApiItemId.HasValue && remoteIds.Contains(item.PokeApiItemId.Value))
            .ToDictionaryAsync(item => item.PokeApiItemId!.Value, cancellationToken);
        var added = 0;
        var updated = 0;

        foreach (var remote in remoteItems)
        {
            if (localItems.TryGetValue(remote.Id, out var local))
            {
                local.Nombre = remote.Nombre;
                local.UrlImagen = remote.UrlImagen;
                local.Descripcion = remote.Descripcion;
                local.Efecto = "captura";
                local.Valor = remote.Valor;
                // La probabilidad es una regla del juego y se conserva si ya fue configurada.
                updated++;
                continue;
            }

            context.Items.Add(new Item
            {
                PokeApiItemId = remote.Id,
                Nombre = remote.Nombre,
                UrlImagen = remote.UrlImagen,
                Descripcion = remote.Descripcion,
                Efecto = "captura",
                Valor = remote.Valor,
                Probabilidad = DefaultCaptureProbability(remote.Nombre)
            });
            added++;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new SincronizacionItemsDto(added, updated, remoteItems.Count);
    }

    private static float DefaultCaptureProbability(string name) => name switch
    {
        "master-ball" => 1f,
        "ultra-ball" => .70f,
        "great-ball" => .50f,
        "quick-ball" or "dusk-ball" or "timer-ball" or "repeat-ball" or "net-ball" or "dive-ball" => .45f,
        _ => .35f
    };
}
