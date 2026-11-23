
using ArenaKuis.API.Data;
using ArenaKuis.API.DTOs;
using ArenaKuis.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Security.Cryptography;

namespace ArenaKuis.API.Controllers;

[ApiController]
[Route("api/classrooms")]
[Authorize]
public class ClassroomsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClassroomsController(AppDbContext db)
    {
        _db = db;
    }

    

    [Authorize(Roles = "Teacher,Admin")]
    [HttpPost]
    public IActionResult CreateClassroom(
        CreateClassroomRequest? request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Nama kelas wajib diisi.");
        }

        string name = request.Name.Trim();

        if (name.Length < 2 || name.Length > 100)
        {
            return BadRequest(
                "Nama kelas harus 2 sampai 100 karakter."
            );
        }

        var teacherId = AmbilUserId();

        if (teacherId == null)
        {
            return Unauthorized();
        }

        // Pastikan akun yang membuat kelas masih ada.
        var teacher = _db.Users.Find(teacherId.Value);

        if (teacher == null)
        {
            return Unauthorized();
        }

        string classCode = BuatKodeKelas();

        var classroom = new Classroom
        {
            TeacherId = teacherId.Value,
            Name = name,
            ClassCode = classCode,
            StudentsIds = new List<int>()
        };

        _db.Classrooms.Add(classroom);
        _db.SaveChanges();

        // Jangan mengirim daftar ID siswa ke client.
        return Created(
            $"/api/classrooms/{classroom.Id}",
            new
            {
                classroom.Id,
                classroom.Name,
                classroom.TeacherId,
                classroom.ClassCode,
                StudentCount = 0
            }
        );
    }

    

    [Authorize(Roles = "Student")]
    [EnableRateLimiting("JoinClassLimit")]
    [HttpPost("join")]
    public IActionResult JoinClassroom(
        JoinClassroomRequest? request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.ClassCode))
        {
            return BadRequest("Kode kelas wajib diisi.");
        }

        string classCode = request.ClassCode
            .Trim()
            .ToUpperInvariant();

        if (classCode.Length != 8)
        {
            return BadRequest("Format kode kelas tidak valid.");
        }

        var classroom = _db.Classrooms.FirstOrDefault(
            classroom => classroom.ClassCode == classCode
        );

        if (classroom == null)
        {
            return NotFound("Kode kelas tidak ditemukan.");
        }

        var studentId = AmbilUserId();

        if (studentId == null)
        {
            return Unauthorized();
        }

        // Pastikan siswa masih terdaftar di database.
        var student = _db.Users.Find(studentId.Value);

        if (student == null)
        {
            return Unauthorized();
        }

        // Cegah siswa bergabung berulang kali.
        if (classroom.StudentsIds.Contains(studentId.Value))
        {
            return Conflict(
                "Kamu sudah bergabung ke kelas ini."
            );
        }

        classroom.StudentsIds.Add(studentId.Value);

        _db.SaveChanges();

        return Ok("Berhasil bergabung ke kelas.");
    }

    

    [HttpGet]
    public IActionResult GetMyClassrooms()
    {
        var userId = AmbilUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var user = _db.Users.Find(userId.Value);

        if (user == null)
        {
            return Unauthorized();
        }

        // ADMIN
        if (User.IsInRole("Admin"))
        {
            var classrooms = _db.Classrooms
                .Select(classroom => new
                {
                    classroom.Id,
                    classroom.Name,
                    classroom.TeacherId,
                    classroom.ClassCode,
                    StudentCount = classroom.StudentsIds.Count
                })
                .ToList();

            return Ok(classrooms);
        }

        // GURU
        if (User.IsInRole("Teacher"))
        {
            var classrooms = _db.Classrooms
                .Where(classroom =>
                    classroom.TeacherId == userId.Value)
                .Select(classroom => new
                {
                    classroom.Id,
                    classroom.Name,
                    classroom.TeacherId,
                    classroom.ClassCode,
                    StudentCount = classroom.StudentsIds.Count
                })
                .ToList();

            return Ok(classrooms);
        }

        // SISWA
        if (User.IsInRole("Student"))
        {
            var classrooms = _db.Classrooms
                .Where(classroom =>
                    classroom.StudentsIds.Contains(userId.Value))
                .Select(classroom => new
                {
                    classroom.Id,
                    classroom.Name,
                    classroom.TeacherId,
                    StudentCount = classroom.StudentsIds.Count

                    // ClassCode tidak dikirim kepada siswa.
                    // StudentsIds juga tidak dikirim.
                })
                .ToList();

            return Ok(classrooms);
        }

        return Forbid();
    }

    

    private string BuatKodeKelas()
    {
        const string characters =
            "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        string code;

        do
        {
            code = string.Concat(
                Enumerable.Range(0, 8)
                    .Select(_ =>
                        characters[
                            RandomNumberGenerator.GetInt32(
                                characters.Length
                            )
                        ]
                    )
            );
        }
        while (_db.Classrooms.Any(
            classroom => classroom.ClassCode == code
        ));

        return code;
    }

    

    private int? AmbilUserId()
    {
        string? id = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (!int.TryParse(id, out int userId))
        {
            return null;
        }

        return userId;
    }
}
