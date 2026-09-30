using BackPokemon.Models;
using Microsoft.AspNetCore.Mvc;

namespace BackPokemon.Controllers;

[ApiController]
[Route("api/Estados")]
public sealed class EstadosController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        var estados = Enum.GetValues<EstadoPokemon>()
            .OrderBy(estado => (int)estado)
            .Select(estado => new
            {
                Id = (int)estado,
                Nombre = estado.ToString()
            });

        return Ok(estados);
    }
}
