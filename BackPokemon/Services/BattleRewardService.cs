using BackPokemon.Data;
using BackPokemon.Models;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Services;

public sealed class BattleRewardService(ApplicationDbContext context)
{
    public async Task<RecompensaBatalla?> AwardRandomItem(int battleId, string userId,
        CancellationToken cancellationToken)
    {
        var existing = await context.RecompensasBatalla.FirstOrDefaultAsync(value => value.BatallaId == battleId,
            cancellationToken);
        if (existing is not null) return existing;

        var itemIds = await context.EfectosItems.Where(effect => effect.Tipo == "ball" || effect.Tipo == "berry")
            .Select(effect => effect.ItemId).ToListAsync(cancellationToken);
        if (itemIds.Count == 0) return null;
        var itemId = itemIds[Random.Shared.Next(itemIds.Count)];
        var inventory = await context.ItemUsers.FirstOrDefaultAsync(value => value.IdUser == userId
            && value.IdItem == itemId, cancellationToken);
        if (inventory is null)
        {
            inventory = new ItemUser { IdUser = userId, IdItem = itemId, Cantidad = 0 };
            context.ItemUsers.Add(inventory);
        }
        inventory.Cantidad++;
        var reward = new RecompensaBatalla
        {
            BatallaId = battleId, UsuarioId = userId, ItemId = itemId, Cantidad = 1,
            Fecha = DateTime.UtcNow
        };
        context.RecompensasBatalla.Add(reward);
        return reward;
    }
}
