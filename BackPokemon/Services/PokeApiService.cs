using System.Net;
using System.Text.Json;

namespace BackPokemon.Services;

public sealed class PokeApiService(IHttpClientFactory httpClientFactory)
{
    public async Task<PokeApiPokemon> GetPokemon(int pokemonId, CancellationToken cancellationToken)
    {
        using var response = await httpClientFactory.CreateClient("PokeApi")
            .GetAsync($"pokemon/{pokemonId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new PokeApiPokemonNotFoundException();

        if (!response.IsSuccessStatusCode)
            throw new PokeApiUnavailableException();

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = document.RootElement;
        var types = root.GetProperty("types").EnumerateArray()
            .OrderBy(type => type.GetProperty("slot").GetInt32())
            .Select(type => type.GetProperty("type").GetProperty("name").GetString()!)
            .ToArray();
        var stats = root.GetProperty("stats").EnumerateArray()
            .ToDictionary(
                stat => stat.GetProperty("stat").GetProperty("name").GetString()!,
                stat => stat.GetProperty("base_stat").GetInt32());
        var sprites = root.GetProperty("sprites");

        return new PokeApiPokemon(
            root.GetProperty("id").GetInt32(),
            root.GetProperty("name").GetString()!,
            stats["hp"], stats["attack"], stats["defense"], stats["speed"],
            types[0], types.Length > 1 ? types[1] : null,
            SpriteOrFallback(sprites, "front_default", pokemonId),
            SpriteOrFallback(sprites, "back_default", pokemonId));
    }

    public async Task<IReadOnlyList<PokeApiItem>> GetPokeBallItems(CancellationToken cancellationToken)
    {
        using var categoryResponse = await httpClientFactory.CreateClient("PokeApi")
            .GetAsync("item-category/poke-balls", cancellationToken);
        if (!categoryResponse.IsSuccessStatusCode)
            throw new PokeApiUnavailableException();

        using var categoryDocument = await JsonDocument.ParseAsync(
            await categoryResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var urls = categoryDocument.RootElement.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("url").GetString()!)
            .ToArray();
        var items = new List<PokeApiItem>(urls.Length);

        // PokéAPI entrega el catálogo y el detalle por rutas distintas. Se limita
        // la concurrencia para no saturar el servicio externo.
        using var semaphore = new SemaphoreSlim(4);
        var requests = urls.Select(async url =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try { return await GetItem(new Uri(url), cancellationToken); }
            finally { semaphore.Release(); }
        });
        foreach (var item in await Task.WhenAll(requests)) items.Add(item);
        return items.OrderBy(item => item.Id).ToArray();
    }

    private async Task<PokeApiItem> GetItem(Uri url, CancellationToken cancellationToken)
    {
        using var response = await httpClientFactory.CreateClient("PokeApi").GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new PokeApiUnavailableException();
        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = document.RootElement;
        var description = root.GetProperty("effect_entries").EnumerateArray()
            .FirstOrDefault(entry => entry.GetProperty("language").GetProperty("name").GetString() == "en");
        var effect = description.ValueKind == JsonValueKind.Undefined
            ? "Poké Ball"
            : description.GetProperty("short_effect").GetString() ?? "Poké Ball";
        var sprites = root.GetProperty("sprites");

        return new PokeApiItem(
            root.GetProperty("id").GetInt32(),
            root.GetProperty("name").GetString()!,
            root.GetProperty("cost").GetInt32(),
            effect,
            sprites.TryGetProperty("default", out var sprite) ? sprite.GetString() ?? string.Empty : string.Empty);
    }

    private static string SpriteOrFallback(JsonElement sprites, string property, int pokemonId)
    {
        if (sprites.TryGetProperty(property, out var sprite) && sprite.GetString() is { Length: > 0 } url)
            return url;

        var side = property.StartsWith("back", StringComparison.Ordinal) ? "back/" : string.Empty;
        return $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/{side}{pokemonId}.png";
    }
}

public sealed record PokeApiPokemon(
    int Id, string Nombre, int Vida, int Ataque, int Defensa, int Velocidad,
    string TipoPrimario, string? TipoSecundario, string UrlFrontal, string UrlTrasera);

public sealed record PokeApiItem(int Id, string Nombre, int Valor, string Descripcion, string UrlImagen);

public sealed class PokeApiPokemonNotFoundException : Exception;
public sealed class PokeApiUnavailableException : Exception;
