
using ArenaKuis.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ArenaKuis.API.Controllers;

[ApiController]
[Route("api/results")]
[Authorize]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResultsController(AppDbContext db)
    {
        _db = db;
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId
        );
    }

    [HttpGet("my-results")]
    [Authorize(Roles = "Student")]
    public IActionResult GetMyResults()
    {
        if (!TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        var results = _db.QuizAttempts
            .Where(attempt => attempt.StudentId == userId)
            .OrderByDescending(attempt => attempt.CompletedAt)
            .Select(attempt => new
            {
                attempt.Id,
                attempt.QuizId,
                attempt.Score,
                attempt.CorrectAnswer,
                attempt.TotalQuestions,
                attempt.XP,
                attempt.CompletedAt
            })
            .ToList();

        return Ok(results);
    }

    [HttpGet("teacher-results")]
    [Authorize(Roles = "Teacher,Admin")]
    public IActionResult GetTeacherResults()
    {
        if (!TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        var query = _db.QuizAttempts
            .AsQueryable();

        if (User.IsInRole("Teacher"))
        {
            query = query.Where(attempt =>
                _db.Quizzes.Any(quiz =>
                    quiz.Id == attempt.QuizId &&
                    quiz.TeacherId == userId
                )
            );
        }

        var results = query
            .OrderByDescending(attempt => attempt.CompletedAt)
            .Select(attempt => new
            {
                attempt.Id,
                attempt.StudentId,
                attempt.QuizId,
                attempt.Score,
                attempt.CorrectAnswer,
                attempt.TotalQuestions,
                attempt.XP,
                attempt.CompletedAt
            })
            .ToList();

        return Ok(results);
    }

    [HttpGet("leaderboard")]
    [AllowAnonymous]
    public IActionResult GetLeaderboard()
    {
        var leaderboard = _db.Users
            .Where(user => user.Role == "Student")
            .OrderByDescending(user => user.XP)
            .ThenBy(user => user.Username)
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
