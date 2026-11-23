
using ArenaKuis.API.Data;
using ArenaKuis.API.DTOs;
using ArenaKuis.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArenaKuis.API.Controllers;

[ApiController]
[Route("api/questions")]
public class QuestionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public QuestionsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetQuestions(
        string? language,
        string? category)
    {
        var query = _db.Questions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(language))
        {
            query = query.Where(
                question => question.Language == language
            );
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(
                question => question.Category == category
            );
        }

        return Ok(query.ToList());
    }

    [Authorize(Roles = "Teacher,Admin")]
    [HttpPost]
    public IActionResult CreateQuestion(CreateQuestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Language) ||
            string.IsNullOrWhiteSpace(request.Category) ||
            string.IsNullOrWhiteSpace(request.QuestionText))
        {
            return BadRequest("Data soal wajib diisi.");
        }

        if (request.CorrectAnswer < 0 ||
            request.CorrectAnswer > 3)
        {
            return BadRequest("Jawaban harus antara 0 sampai 3.");
        }

        var teacherId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var question = new Question
        {
            TeacherId = teacherId,
            Language = request.Language,
            Category = request.Category,
            QuestionText = request.QuestionText,
            OptionA = request.OptionA,
            OptionB = request.OptionB,
            OptionC = request.OptionC,
            OptionD = request.OptionD,
            CorrectAnswer = request.CorrectAnswer,
            Difficulty = request.Difficulty
        };

        _db.Questions.Add(question);

        _db.SaveChanges();

        return Ok(question);
    }

    [Authorize(Roles = "Teacher,Admin")]
    [HttpPut("{id}")]
    public IActionResult UpdateQuestion(
        int id,
        CreateQuestionRequest request)
    {
        var question = _db.Questions.Find(id);

        if (question == null)
        {
            return NotFound("Soal tidak ditemukan.");
        }

        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        if (User.IsInRole("Teacher") &&
            question.TeacherId != userId)
        {
            return Forbid();
        }

        question.Language = request.Language;
        question.Category = request.Category;
        question.QuestionText = request.QuestionText;
        question.OptionA = request.OptionA;
        question.OptionB = request.OptionB;
        question.OptionC = request.OptionC;
        question.OptionD = request.OptionD;
        question.CorrectAnswer = request.CorrectAnswer;
        question.Difficulty = request.Difficulty;

        _db.SaveChanges();

        return Ok("Soal berhasil diperbarui.");
    }

    [Authorize(Roles = "Teacher,Admin")]
    [HttpDelete("{id}")]
    public IActionResult DeleteQuestion(int id)
    {
        var question = _db.Questions.Find(id);

        if (question == null)
        {
            return NotFound();
        }

        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        if (User.IsInRole("Teacher") &&
            question.TeacherId != userId)
        {
            return Forbid();
        }

        _db.Questions.Remove(question);

        _db.SaveChanges();

        return Ok("Soal berhasil dihapus.");
    }
}
