using System.Security.Claims;
using BackPokemon.Data;
using BackPokemon.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace BackPokemon.Controllers;
[Authorize]
[ApiController]
[Route("users")]
[Route("api/users")]
public class UsersController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();
        search = search?.Trim();
        if (search is null || search.Length < 2 || search.Length > 100)
            return BadRequest("search debe tener entre 2 y 100 caracteres.");
        var normalized = search.ToUpperInvariant();
        return Ok(await context.Users.AsNoTracking()
            .Where(u => u.Id != userId && u.NormalizedUserName != null && u.NormalizedUserName.Contains(normalized))
            .OrderBy(u => u.UserName).Take(50)
            .Select(u => new PublicUserDto(u.Id, u.UserName)).ToListAsync(cancellationToken));
    }
}
