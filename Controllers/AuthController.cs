
using ArenaKuis.API.Data;
using ArenaKuis.API.DTOs;
using ArenaKuis.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ArenaKuis.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<User> _hasher;
    private readonly IConfiguration _config;

    public AuthController(
        AppDbContext db,
        PasswordHasher<User> hasher,
        IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _config = config;
    }

    [HttpPost("register")]
    public IActionResult Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Nama) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Semua data wajib diisi.");
        }

        if (request.Username.Length < 3)
        {
            return BadRequest("Username minimal 3 karakter.");
        }

        if (request.Password.Length < 8)
        {
            return BadRequest("Password minimal 8 karakter.");
        }

        var usernameExists = _db.Users.Any(
            user => user.Username == request.Username
        );

        if (usernameExists)
        {
            return Conflict("Username sudah digunakan.");
        }

        var user = new User
        {
            Username = request.Username,
            Nama = request.Nama,
            Role = "Student",
            XP = 0
        };

        user.PasswordHash = _hasher.HashPassword(
            user,
            request.Password
        );

        _db.Users.Add(user);

        _db.SaveChanges();

        return Ok("Registrasi berhasil.");
    }

    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        var user = _db.Users.FirstOrDefault(
            user => user.Username == request.Username
        );

        if (user == null)
        {
            return Unauthorized("Username atau password salah.");
        }

        var result = _hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );

        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Username atau password salah.");
        }

        var token = BuatToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            Username = user.Username,
            Nama = user.Nama,
            Role = user.Role,
            XP = user.XP
        });
    }

    private string BuatToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
