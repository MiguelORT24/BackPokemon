using System.Data;
using BackPokemon.Data;
using BackPokemon.Models;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Services;

/// <summary>Applies elapsed passive ball grants when a player next uses the game.</summary>
public sealed class BallReplenishmentService(ApplicationDbContext context)
{
    private static readonly TimeSpan ReplenishmentInterval = TimeSpan.FromMinutes(30);
    private const int MaxPerBall = 100;
    private const int BasicBallsPerInterval = 10;
    private const int RandomBallsPerInterval = 5;

    public async Task ReplenishIfDue(string userId, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        // Lock this user's row so parallel inventory/map requests cannot grant the same interval twice.
        var user = await context.Users.FromSqlInterpolated($"SELECT * FROM [AspNetUsers] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {userId}")
            .SingleOrDefaultAsync(cancellationToken);
        if (user is null) return;

        var now = DateTime.UtcNow;
        var previousGrant = user.UltimaRecargaPokebolas;
        var intervalsDue = previousGrant is null
            ? 1
            : (int)Math.Min(int.MaxValue, (now - previousGrant.Value).Ticks / ReplenishmentInterval.Ticks);
        if (intervalsDue <= 0) return;

        var basic = await FindBall("poke-ball", 1f, cancellationToken);
        var super = await FindBall("great-ball", 1.5f, cancellationToken);
        var ultra = await FindBall("ultra-ball", 2f, cancellationToken);
        // Do not consume a user's first interval until the catalog and all three game rules exist.
        if (basic is null || super is null || ultra is null) return;

        var balls = new[] { basic, super, ultra };
        var itemIds = balls.Select(item => item.Id).ToArray();
        var inventory = await context.ItemUsers.Where(row => row.IdUser == userId && itemIds.Contains(row.IdItem))
            .ToDictionaryAsync(row => row.IdItem, cancellationToken);

        int Quantity(Item item) => inventory.TryGetValue(item.Id, out var row) ? row.Cantidad : 0;

        var basicRoom = Math.Max(0, MaxPerBall - Quantity(basic));
        var basicToAdd = (int)Math.Min(basicRoom, (long)BasicBallsPerInterval * intervalsDue);
        Add(basic, basicToAdd);

        // Choose each of the five bonus balls independently. If the selected type is full,
        // redirect that ball to the other type while it still has room.
        var randomBallsDue = (long)RandomBallsPerInterval * intervalsDue;
        var randomCapacity = Math.Max(0, MaxPerBall - Quantity(super)) + Math.Max(0, MaxPerBall - Quantity(ultra));
        var randomBallsToAdd = (int)Math.Min(randomBallsDue, randomCapacity);
        for (var index = 0; index < randomBallsToAdd; index++)
        {
            var superRoom = Quantity(super) < MaxPerBall;
            var ultraRoom = Quantity(ultra) < MaxPerBall;
            if (superRoom && ultraRoom)
                Add(Random.Shared.Next(2) == 0 ? super : ultra, 1);
            else
                Add(superRoom ? super : ultra, 1);
        }

        user.UltimaRecargaPokebolas = previousGrant is null
            ? now
            : previousGrant.Value.AddTicks(ReplenishmentInterval.Ticks * (long)intervalsDue);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        void Add(Item item, int amount)
        {
            if (amount <= 0) return;
            if (!inventory.TryGetValue(item.Id, out var row))
            {
                row = new ItemUser { IdUser = userId, IdItem = item.Id, Cantidad = 0 };
                inventory.Add(item.Id, row);
                context.ItemUsers.Add(row);
            }
            row.Cantidad = Math.Min(MaxPerBall, row.Cantidad + amount);
        }
    }

    private async Task<Item?> FindBall(string name, float multiplier, CancellationToken cancellationToken)
    {
        return await context.Items.Include(item => item.EfectoJuego)
            .FirstOrDefaultAsync(item => item.Nombre == name
                && item.EfectoJuego != null
                && item.EfectoJuego.Tipo == "ball"
                && item.EfectoJuego.MultiplicadorCaptura == multiplier,
                cancellationToken);
    }
}
