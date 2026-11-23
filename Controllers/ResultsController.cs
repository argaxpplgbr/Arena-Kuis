
using ArenaKuis.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArenaKuis.API.Controllers;

[ApiController]
[Route("api/results")]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResultsController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpGet("my-results")]
    public IActionResult GetMyResults()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var results = _db.QuizAttempts
            .Where(attempt => attempt.StudentId == userId)
            .OrderByDescending(attempt => attempt.CompletedAt)
            .ToList();

        return Ok(results);
    }

    [Authorize(Roles = "Teacher,Admin")]
    [HttpGet("teacher-results")]
    public IActionResult GetTeacherResults()
    {
        var teacherId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var quizIds = _db.Quizzes
            .Where(quiz => quiz.TeacherId == teacherId)
            .Select(quiz => quiz.Id)
            .ToList();

        var results = _db.QuizAttempts
            .Where(attempt => quizIds.Contains(attempt.QuizId))
            .ToList();

        return Ok(results);
    }

    [HttpGet("leaderboard")]
    public IActionResult GetLeaderboard()
    {
        var leaderboard = _db.Users
            .Where(user => user.Role == "Student")
            .OrderByDescending(user => user.XP)
            .Select(user => new
            {
                user.Username,
                user.Nama,
                user.XP
            })
            .Take(100)
            .ToList();

        return Ok(leaderboard);
    }
}
