using BackPokemon.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BackPokemon.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<PokemonUser> PokemonUsers => Set<PokemonUser>();
    public DbSet<Pokedex> Pokedex => Set<Pokedex>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemUser> ItemUsers => Set<ItemUser>();
    public DbSet<PokemonEstado> PokemonEstados => Set<PokemonEstado>();
    public DbSet<PokemonCrianza> PokemonCrianzas => Set<PokemonCrianza>();
    public DbSet<Intercambio> Intercambios => Set<Intercambio>();
    public DbSet<Batalla> Batallas => Set<Batalla>();
    public DbSet<Captura> Capturas => Set<Captura>();
    public DbSet<EfectoItem> EfectosItems => Set<EfectoItem>();
    public DbSet<Encuentro> Encuentros => Set<Encuentro>();
    public DbSet<EncuentroUsuario> EncuentrosUsuario => Set<EncuentroUsuario>();
    public DbSet<ObjetoCampo> ObjetosCampo => Set<ObjetoCampo>();
    public DbSet<RecompensaBatalla> RecompensasBatalla => Set<RecompensaBatalla>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PokemonUser>(entity =>
        {
            entity.HasKey(pokemonUser => pokemonUser.Id);

            entity.HasOne(pokemonUser => pokemonUser.Usuario)
                .WithMany(user => user.PokemonUsers)
                .HasForeignKey(pokemonUser => pokemonUser.IdUsuario)
                .HasPrincipalKey(user => user.Id)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PokemonEstado>(entity =>
        {
            entity.HasKey(estado => estado.IdPokemonUser);

            entity.HasOne(estado => estado.PokemonUser)
                .WithOne(pokemonUser => pokemonUser.EstadoDetalle)
                .HasForeignKey<PokemonEstado>(estado => estado.IdPokemonUser)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PokemonCrianza>(entity =>
        {
            entity.HasKey(crianza => crianza.Id);

            entity.HasIndex(crianza => crianza.IdPokemonUser)
                .IsUnique();

            entity.HasOne(crianza => crianza.PokemonUser)
                .WithOne(pokemonUser => pokemonUser.Crianza)
                .HasForeignKey<PokemonCrianza>(crianza => crianza.IdPokemonUser)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pokedex>(entity =>
        {
            entity.HasKey(pokedex => pokedex.Id);

            entity.HasOne(pokedex => pokedex.Usuario)
                .WithMany(user => user.PokedexEntries)
                .HasForeignKey(pokedex => pokedex.UserId)
                .HasPrincipalKey(user => user.Id)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(pokedex => new { pokedex.UserId, pokedex.PokemonId }).IsUnique();
        });

        modelBuilder.Entity<Captura>(entity =>
        {
            entity.HasKey(captura => captura.Id);
            entity.Property(captura => captura.ProbabilidadAplicada).HasColumnType("real");
            entity.HasIndex(captura => new { captura.UsuarioId, captura.Fecha });
            entity.HasOne(captura => captura.Usuario).WithMany(user => user.Capturas)
                .HasForeignKey(captura => captura.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(captura => captura.Pokeball).WithMany()
                .HasForeignKey(captura => captura.IdPokeball).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(captura => captura.PokemonUser).WithMany(pokemon => pokemon.Capturas)
                .HasForeignKey(captura => captura.IdPokemonUser).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(captura => captura.Encuentro).WithMany(encuentro => encuentro.Capturas)
                .HasForeignKey(captura => captura.EncuentroId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EfectoItem>(entity =>
        {
            entity.HasKey(effect => effect.ItemId);
            entity.Property(effect => effect.MultiplicadorCaptura).HasColumnType("real");
            entity.HasOne(effect => effect.Item).WithOne(item => item.EfectoJuego)
                .HasForeignKey<EfectoItem>(effect => effect.ItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Encuentro>(entity =>
        {
            entity.HasKey(encounter => encounter.Id);
            entity.HasIndex(encounter => encounter.ZonaKey).IsUnique();
            entity.HasIndex(encounter => encounter.FechaExpiracion);
            entity.HasIndex(encounter => new { encounter.Latitud, encounter.Longitud });
            entity.HasIndex(encounter => encounter.UsuarioId);
            entity.HasOne(encounter => encounter.Usuario).WithMany()
                .HasForeignKey(encounter => encounter.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EncuentroUsuario>(entity =>
        {
            entity.HasKey(state => new { state.EncuentroId, state.UsuarioId });
            entity.HasIndex(state => new { state.UsuarioId, state.Capturado });
            entity.HasOne(state => state.Encuentro).WithMany(encounter => encounter.EstadosUsuario)
                .HasForeignKey(state => state.EncuentroId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(state => state.Usuario).WithMany()
                .HasForeignKey(state => state.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ObjetoCampo>(entity =>
        {
            entity.HasKey(drop => drop.Id);
            entity.HasOne(drop => drop.Item).WithMany().HasForeignKey(drop => drop.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(drop => new { drop.RecogidoPorUsuarioId, drop.FechaExpiracion });
        });

        modelBuilder.Entity<RecompensaBatalla>(entity =>
        {
            entity.HasKey(reward => reward.Id);
            entity.HasIndex(reward => reward.BatallaId).IsUnique();
            entity.HasOne(reward => reward.Batalla).WithOne(battle => battle.Recompensa)
                .HasForeignKey<RecompensaBatalla>(reward => reward.BatallaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(reward => reward.Usuario).WithMany(user => user.RecompensasBatalla)
                .HasForeignKey(reward => reward.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(reward => reward.Item).WithMany().HasForeignKey(reward => reward.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Probabilidad).HasColumnType("real");
            entity.HasIndex(item => item.PokeApiItemId).IsUnique();
        });

        modelBuilder.Entity<ItemUser>(entity =>
        {
            entity.HasKey(itemUser => itemUser.Id);

            entity.HasOne(itemUser => itemUser.Item)
                .WithMany(item => item.ItemUsers)
                .HasForeignKey(itemUser => itemUser.IdItem)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(itemUser => itemUser.Usuario)
                .WithMany(user => user.ItemUsers)
                .HasForeignKey(itemUser => itemUser.IdUser)
                .HasPrincipalKey(user => user.Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(itemUser => new { itemUser.IdItem, itemUser.IdUser })
                .IsUnique();
        });

        modelBuilder.Entity<Intercambio>(entity =>
        {
            entity.HasKey(intercambio => intercambio.Id);
            entity.HasOne(t => t.UsuarioCreador).WithMany()
                .HasForeignKey(t => t.UsuarioCreadorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.UsuarioDestinatario).WithMany()
                .HasForeignKey(t => t.UsuarioDestinatarioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(t => new { t.Estado, t.PokemonUserR });
            entity.HasIndex(t => new { t.Estado, t.PokemonUserD });

            entity.HasOne(intercambio => intercambio.PokemonUserRemitente)
                .WithMany(pokemonUser => pokemonUser.IntercambiosComoRemitente)
                .HasForeignKey(intercambio => intercambio.PokemonUserR)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(intercambio => intercambio.PokemonUserDestinatario)
                .WithMany(pokemonUser => pokemonUser.IntercambiosComoDestinatario)
                .HasForeignKey(intercambio => intercambio.PokemonUserD)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Batalla>(entity =>
        {
            entity.HasKey(batalla => batalla.Id);
            entity.HasIndex(batalla => new { batalla.IdUsuario, batalla.Fecha });
            entity.HasOne(batalla => batalla.Usuario).WithMany()
                .HasForeignKey(batalla => batalla.IdUsuario).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(batalla => batalla.LlaveIdempotencia).IsUnique()
                .HasFilter("[LlaveIdempotencia] IS NOT NULL");

            entity.HasOne(batalla => batalla.PokemonUserRetador)
                .WithMany(pokemonUser => pokemonUser.BatallasComoRetador)
                .HasForeignKey(batalla => batalla.PokemonUserR)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
