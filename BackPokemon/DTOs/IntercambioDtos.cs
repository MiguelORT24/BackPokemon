using System.ComponentModel.DataAnnotations;
namespace BackPokemon.DTOs;
public class CreateTradeDto
{
    [Required] public string RecipientUserId { get; set; } = null!;
    [Range(1, int.MaxValue)] public int PokemonUserId { get; set; }
}
public class AcceptTradeDto
{
    [Range(1, int.MaxValue)] public int PokemonUserId { get; set; }
}
public record PublicUserDto(string Id, string? UserName);
public record TradePokemonDto(int Id, int IdPokemon, string? Nombre);
public record TradeDto(int Id, string Role, string Status, PublicUserDto Creator,
    PublicUserDto Recipient, TradePokemonDto OfferedPokemon, TradePokemonDto? RecipientPokemon,
    DateTime CreatedAt, DateTime? AcceptedAt, DateTime? ConfirmedAt);
