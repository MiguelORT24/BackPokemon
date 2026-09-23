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
    public DbSet<Intercambio> Intercambios => Set<Intercambio>();
    public DbSet<Batalla> Batallas => Set<Batalla>();

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

        modelBuilder.Entity<Pokedex>(entity =>
        {
            entity.HasKey(pokedex => pokedex.Id);

            entity.HasOne(pokedex => pokedex.Usuario)
                .WithMany(user => user.PokedexEntries)
                .HasForeignKey(pokedex => pokedex.UserId)
                .HasPrincipalKey(user => user.Id)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Probabilidad).HasColumnType("real");
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

            entity.HasOne(batalla => batalla.PokemonUserRetador)
                .WithMany(pokemonUser => pokemonUser.BatallasComoRetador)
                .HasForeignKey(batalla => batalla.PokemonUserR)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(batalla => batalla.PokemonUserContrincante)
                .WithMany(pokemonUser => pokemonUser.BatallasComoContrincante)
                .HasForeignKey(batalla => batalla.PokemonUserC)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(batalla => batalla.PokemonUserGanadorNavigation)
                .WithMany(pokemonUser => pokemonUser.BatallasGanadas)
                .HasForeignKey(batalla => batalla.PokemonUserGanador)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
