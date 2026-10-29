using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using BackPokemon.Data;
using BackPokemon.DTOs;
using BackPokemon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BackPokemon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ApplicationDbContext context,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = new ApplicationUser { UserName = request.UserName, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);
        return Ok(new { message = "Usuario registrado correctamente" });
    }

    // Only an existing administrator can create accounts and assign/create roles.
    [Authorize(Roles = "GameAdmin")]
    [HttpPost("register-with-roles")]
    public async Task<IActionResult> RegisterWithRoles(RegisterWithRolesRequest request,
        CancellationToken cancellationToken)
    {
        var roleNames = request.Roles.Select(role => role?.Trim()).Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (roleNames.Length == 0)
            return BadRequest("Indica al menos un rol.");
        if (roleNames.Any(role => !Regex.IsMatch(role, "^[A-Za-z][A-Za-z0-9_-]{0,31}$")))
            return BadRequest("Cada rol debe iniciar con una letra y contener hasta 32 letras, números, guiones o guiones bajos.");
        if (await userManager.FindByNameAsync(request.UserName) is not null)
            return Conflict("El nombre de usuario ya existe.");
        if (await userManager.FindByEmailAsync(request.Email) is not null)
            return Conflict("El correo ya está registrado.");

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        foreach (var roleName in roleNames)
        {
            if (await roleManager.RoleExistsAsync(roleName)) continue;
            var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!roleResult.Succeeded)
                return BadRequest(roleResult.Errors);
        }

        var user = new ApplicationUser { UserName = request.UserName, Email = request.Email };
        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded) return BadRequest(createResult.Errors);

        var addRolesResult = await userManager.AddToRolesAsync(user, roleNames);
        if (!addRolesResult.Succeeded) return BadRequest(addRolesResult.Errors);
        await transaction.CommitAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new
        {
            id = user.Id,
            userName = user.UserName,
            roles = roleNames
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await userManager.FindByNameAsync(request.UserName);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { message = "Usuario o contraseña incorrectos" });

        var roles = await userManager.GetRolesAsync(user);
        var (token, expiration) = GenerateToken(user, roles);
        return Ok(new { token, expiration });
    }

    private (string Token, DateTime Expiration) GenerateToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Falta configurar Jwt:Key.");
        var minutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "60");
        var expiration = DateTime.UtcNow.AddMinutes(minutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(configuration["Jwt:Issuer"], configuration["Jwt:Audience"], claims,
            expires: expiration, signingCredentials: credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expiration);
    }
}