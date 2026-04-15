using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SecureFleet.Api.Models;
using SecureFleet.Api.Services;

namespace SecureFleet.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IDataStore _store;
    public AuthController(IConfiguration config, IDataStore store)
    {
        _config = config;
        _store = store;
    }

    public record DemoLoginRequest(string Email);

    [HttpPost("demo-login")]
    public async Task<IActionResult> DemoLogin([FromBody] DemoLoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { error = "email is required" });

        Profile? profile;
        try
        {
            profile = await _store.GetProfileByEmailAsync(req.Email);
        }
        catch (NpgsqlException ex)
        {
            var msg = ex.Message.Contains("Failed to connect")
                ? "Cannot reach Supabase — check ConnectionString (use the pooler host if on free tier)."
                : "Database error — verify schema and migration_users.sql have been applied.";
            return StatusCode(503, new { error = msg, detail = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { error = "Could not reach the database.", detail = ex.Message });
        }

        if (profile is null)
            return Unauthorized(new { error = "no account for this email — ask your admin to create one" });

        var secret = _config["Supabase:JwtSecret"] ?? "dev-secret-change-me-dev-secret-change-me";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", profile.Id.ToString()),
            new Claim("email", profile.Email),
            new Claim("user_role", profile.Role)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new { token = jwt, role = profile.Role, email = profile.Email, name = profile.FullName });
    }
}
