using System.Data;
using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Services;

public sealed class PokeApiItemSyncService(ApplicationDbContext context, PokeApiService pokeApi)
{
    public async Task<SincronizacionItemsDto> SyncCaptureItems(CancellationToken cancellationToken)
    {
        var remoteItems = await pokeApi.GetCaptureCatalogItems(cancellationToken);
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
                local.Valor = remote.Valor;
                updated++;
                continue;
            }

            context.Items.Add(new Item
            {
                PokeApiItemId = remote.Id,
                Nombre = remote.Nombre,
                UrlImagen = remote.UrlImagen,
                Descripcion = remote.Descripcion,
                Efecto = string.Empty,
                Valor = remote.Valor,
                Probabilidad = 0
            });
            added++;
        }

        await context.SaveChangesAsync(cancellationToken);
        var syncedItems = await context.Items.Where(item => item.PokeApiItemId.HasValue
                && remoteIds.Contains(item.PokeApiItemId.Value))
            .ToDictionaryAsync(item => item.PokeApiItemId!.Value, cancellationToken);
        foreach (var remote in remoteItems)
            await EnsureGameRule(syncedItems[remote.Id], remote.Nombre, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new SincronizacionItemsDto(added, updated, remoteItems.Count);
    }

    private async Task EnsureGameRule(Item item, string name, CancellationToken cancellationToken)
    {
        var type = name == "razz-berry" ? "berry" :
            name is "poke-ball" or "great-ball" or "ultra-ball" ? "ball" : null;
        if (type is null) return;
        var effect = context.EfectosItems.Local.FirstOrDefault(value => value.ItemId == item.Id)
            ?? await context.EfectosItems.FirstOrDefaultAsync(value => value.ItemId == item.Id, cancellationToken);
        if (effect is null)
        {
            effect = new EfectoItem { Item = item, Tipo = type };
            context.EfectosItems.Add(effect);
        }
        effect.Tipo = type;
        effect.MultiplicadorCaptura = type == "ball" ? name switch
        {
            "great-ball" => 1.5f,
            "ultra-ball" => 2f,
            _ => 1f
        } : null;
        effect.DuracionSegundos = type == "berry" ? 30 : null;
    }
}
