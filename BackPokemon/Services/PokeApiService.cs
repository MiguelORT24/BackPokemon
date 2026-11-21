using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace BackPokemon.Services;

public sealed class PokeApiService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
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

        var captureRate = await cache.GetOrCreateAsync($"pokemon-capture-rate:{pokemonId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            var speciesUrl = new Uri(root.GetProperty("species").GetProperty("url").GetString()!);
            using var speciesResponse = await httpClientFactory.CreateClient("PokeApi")
                .GetAsync(speciesUrl, cancellationToken);
            if (!speciesResponse.IsSuccessStatusCode) throw new PokeApiUnavailableException();
            using var speciesDocument = await JsonDocument.ParseAsync(
                await speciesResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            return speciesDocument.RootElement.GetProperty("capture_rate").GetInt32();
        });

        return new PokeApiPokemon(
            root.GetProperty("id").GetInt32(),
            root.GetProperty("name").GetString()!,
            captureRate,
            stats["hp"], stats["attack"], stats["defense"], stats["speed"],
            types[0], types.Length > 1 ? types[1] : null,
            SpriteOrFallback(sprites, "front_default", pokemonId),
            SpriteOrFallback(sprites, "back_default", pokemonId));
    }

    public async Task<IReadOnlyList<PokeApiItem>> GetCaptureCatalogItems(CancellationToken cancellationToken)
    {
        // PokéAPI no tiene categorías "poke-balls" ni "berries" (dan 404). Se parte de los
        // bolsillos "pokeballs" y "berries", que listan sus categorías, y de ahí los objetos.
        var urls = new List<string>();
        foreach (var pocket in new[] { "pokeballs", "berries" })
        {
            using var pocketResponse = await httpClientFactory.CreateClient("PokeApi")
                .GetAsync($"item-pocket/{pocket}", cancellationToken);
            if (!pocketResponse.IsSuccessStatusCode) throw new PokeApiUnavailableException();
            using var pocketDocument = await JsonDocument.ParseAsync(
                await pocketResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var categoryUrls = pocketDocument.RootElement.GetProperty("categories").EnumerateArray()
                .Select(category => category.GetProperty("url").GetString()!)
                .ToArray();
            foreach (var categoryUrl in categoryUrls)
            {
                using var categoryResponse = await httpClientFactory.CreateClient("PokeApi")
                    .GetAsync(new Uri(categoryUrl), cancellationToken);
                if (!categoryResponse.IsSuccessStatusCode) throw new PokeApiUnavailableException();
                using var categoryDocument = await JsonDocument.ParseAsync(
                    await categoryResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                urls.AddRange(categoryDocument.RootElement.GetProperty("items").EnumerateArray()
                    .Select(item => item.GetProperty("url").GetString()!));
            }
        }
        urls = urls.Distinct().ToList();
        var items = new List<PokeApiItem>(urls.Count);

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

        // Algunos objetos de PokéAPI no traen descripción, costo o imagen: se usan valores por defecto.
        var effect = "Poké Ball";
        if (root.TryGetProperty("effect_entries", out var entries) && entries.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in entries.EnumerateArray())
            {
                if (entry.TryGetProperty("language", out var language)
                    && language.TryGetProperty("name", out var languageName)
                    && languageName.GetString() == "en"
                    && entry.TryGetProperty("short_effect", out var shortEffect)
                    && shortEffect.ValueKind == JsonValueKind.String)
                {
                    effect = shortEffect.GetString() ?? effect;
                    break;
                }
            }
        }

        var cost = root.TryGetProperty("cost", out var costElement) && costElement.ValueKind == JsonValueKind.Number
            ? costElement.GetInt32()
            : 0;
        var image = root.TryGetProperty("sprites", out var sprites)
            && sprites.ValueKind == JsonValueKind.Object
            && sprites.TryGetProperty("default", out var sprite)
            && sprite.ValueKind == JsonValueKind.String
                ? sprite.GetString() ?? string.Empty
                : string.Empty;

        return new PokeApiItem(
            root.GetProperty("id").GetInt32(),
            root.GetProperty("name").GetString()!,
            cost,
            effect.Length > 1000 ? effect[..1000] : effect,
            image);
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
    int Id, string Nombre, int CaptureRate, int Vida, int Ataque, int Defensa, int Velocidad,
    string TipoPrimario, string? TipoSecundario, string UrlFrontal, string UrlTrasera);

public sealed record PokeApiItem(int Id, string Nombre, int Valor, string Descripcion, string UrlImagen);

public sealed class PokeApiPokemonNotFoundException : Exception;
public sealed class PokeApiUnavailableException : Exception;
