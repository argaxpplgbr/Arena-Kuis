
using ArenaKuis.API.Data;
using ArenaKuis.API.DTOs;
using ArenaKuis.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArenaKuis.API.Controllers;

[ApiController]
[Route("api/classrooms")]
public class ClassroomsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClassroomsController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize(Roles = "Teacher,Admin")]
    [HttpPost]
    public IActionResult CreateClassroom(CreateClassroomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Nama kelas wajib diisi.");
        }

        var teacherId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var classroom = new Classroom
        {
            TeacherId = teacherId,
            Name = request.Name,
            ClassCode = Guid.NewGuid()
                .ToString("N")[..6]
                .ToUpper()
        };

        _db.Classrooms.Add(classroom);

        _db.SaveChanges();

        return Ok(classroom);
    }

    [Authorize(Roles = "Student")]
    [HttpPost("join")]
    public IActionResult JoinClassroom(JoinClassroomRequest request)
    {
        var classroom = _db.Classrooms.FirstOrDefault(
            classroom => classroom.ClassCode == request.ClassCode
        );

        if (classroom == null)
        {
            return NotFound("Kode kelas tidak ditemukan.");
        }

        var studentId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        if (classroom.StudentsIds.Contains(studentId))
        {
            return Conflict("Kamu sudah bergabung ke kelas ini.");
        }

        classroom.StudentsIds.Add(studentId);

        _db.SaveChanges();

        return Ok("Berhasil bergabung ke kelas.");
    }

    [Authorize]
    [HttpGet]
    public IActionResult GetMyClassrooms()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var user = _db.Users.Find(userId);

        if (user == null)
        {
            return Unauthorized();
        }

        if (user.Role == "Teacher")
        {
            return Ok(
                _db.Classrooms
                    .Where(classroom => classroom.TeacherId == userId)
                    .ToList()
            );
        }

        return Ok(
            _db.Classrooms
                .Where(classroom => classroom.StudentsIds.Contains(userId))
                .ToList()
        );
    }
}
