using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace BackPokemon.Controllers;

[ApiController]
[Route("api")]
public sealed class PokemonController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public PokemonController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("pokemon/{id:int}")]
    public Task<IActionResult> GetPokemon(
        int id,
        CancellationToken cancellationToken)
    {
        return id > 0
            ? GetFromPokeApi($"pokemon/{id}", cancellationToken)
            : Task.FromResult<IActionResult>(BadRequest("El id debe ser mayor que cero."));
    }

    [HttpGet("pokemon")]
    public Task<IActionResult> GetPokemons(
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (offset < 0)
        {
            return Task.FromResult<IActionResult>(
                BadRequest("Offset no puede ser negativo."));
        }

        if (limit is < 1 or > 100)
        {
            return Task.FromResult<IActionResult>(
                BadRequest("Limit debe estar entre 1 y 100."));
        }

        return GetFromPokeApi(
            $"pokemon?offset={offset}&limit={limit}",
            cancellationToken);
    }

    [HttpGet("pokemon-species")]
    public Task<IActionResult> GetPokemonSpecies(
        [FromQuery] int limit = 1,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 100)
        {
            return Task.FromResult<IActionResult>(
                BadRequest("Limit debe estar entre 1 y 100."));
        }

        return GetFromPokeApi(
            $"pokemon-species?limit={limit}",
            cancellationToken);
    }

    [HttpGet("pokemon-species/{id:int}")]
    public Task<IActionResult> GetPokemonSpeciesById(
        int id,
        CancellationToken cancellationToken)
    {
        return id > 0
            ? GetFromPokeApi($"pokemon-species/{id}", cancellationToken)
            : Task.FromResult<IActionResult>(BadRequest("El id debe ser mayor que cero."));
    }

    [HttpGet("generation")]
    public Task<IActionResult> GetGenerations(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 50)
        {
            return Task.FromResult<IActionResult>(
                BadRequest("Limit debe estar entre 1 y 50."));
        }

        return GetFromPokeApi(
            $"generation?limit={limit}",
            cancellationToken);
    }

    [HttpGet("generation/{id:int}")]
    public Task<IActionResult> GetGenerationById(
        int id,
        CancellationToken cancellationToken)
    {
        return id > 0
            ? GetFromPokeApi($"generation/{id}", cancellationToken)
            : Task.FromResult<IActionResult>(BadRequest("El id debe ser mayor que cero."));
    }

    private async Task<IActionResult> GetFromPokeApi(
        string endpoint,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("PokeApi");
            using var response = await client.GetAsync(endpoint, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    "PokeAPI no pudo completar la consulta.");
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken);

            return Ok(json);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(
                StatusCodes.Status504GatewayTimeout,
                "PokeAPI tardó demasiado en responder.");
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                "No se pudo conectar con PokeAPI.");
        }
    }
}
