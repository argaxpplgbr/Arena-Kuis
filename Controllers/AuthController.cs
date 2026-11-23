
using ArenaKuis.API.Data;
using ArenaKuis.API.DTOs;
using ArenaKuis.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

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

    // =========================
    // REGISTER
    // =========================

    [HttpPost("register")]
    [EnableRateLimiting("RegisterLimit")]
    public IActionResult Register(RegisterRequest? request)
    {
        if (request == null)
        {
            return BadRequest("Data registrasi tidak valid.");
        }

        string username = request.Username.Trim().ToLowerInvariant();
        string nama = request.Nama.Trim();
        string password = request.Password;

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(nama) ||
            string.IsNullOrWhiteSpace(password))
        {
            return BadRequest("Semua data wajib diisi.");
        }

        if (username.Length < 3 || username.Length > 32)
        {
            return BadRequest(
                "Username harus memiliki 3 sampai 32 karakter."
            );
        }

        if (!Regex.IsMatch(username, @"^[a-z0-9_-]+$"))
        {
            return BadRequest(
                "Username hanya boleh berisi huruf, angka, underscore, dan tanda minus."
            );
        }

        if (nama.Length < 2 || nama.Length > 60)
        {
            return BadRequest(
                "Nama harus memiliki 2 sampai 60 karakter."
            );
        }

        if (password.Length < 8 || password.Length > 128)
        {
            return BadRequest(
                "Password harus memiliki 8 sampai 128 karakter."
            );
        }

        bool usernameExists = _db.Users.Any(
            user => user.Username == username
        );

        if (usernameExists)
        {
            return Conflict("Username sudah digunakan.");
        }

        var user = new User
        {
            Username = username,
            Nama = nama,
            Role = "Student",
            XP = 0
        };

        user.PasswordHash = _hasher.HashPassword(
            user,
            password
        );

        _db.Users.Add(user);
        _db.SaveChanges();

        return Ok("Registrasi berhasil.");
    }

    // =========================
    // LOGIN
    // =========================

    [HttpPost("login")]
    [EnableRateLimiting("LoginLimit")]
    public IActionResult Login(LoginRequest? request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username dan password wajib diisi.");
        }

        string username = request.Username.Trim().ToLowerInvariant();

        var user = _db.Users.FirstOrDefault(
            user => user.Username == username
        );

        // Pesan sama supaya tidak membocorkan
        // username yang terdaftar.
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

        // Hash password diperbarui otomatis
        // jika algoritma hash membutuhkan peningkatan.
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(
                user,
                request.Password
            );

            _db.SaveChanges();
        }

        string token = BuatToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            Username = user.Username,
            Nama = user.Nama,
            Role = user.Role,
            XP = user.XP
        });
    }

    // =========================
    // PEMBUATAN JWT
    // =========================

    private string BuatToken(User user)
    {
        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()
            ),

            new Claim(
                ClaimTypes.Name,
                user.Username
            ),

            new Claim(
                ClaimTypes.Role,
                user.Role
            )
        };

        string jwtKey = _config["Jwt:Key"]!;

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
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

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}
