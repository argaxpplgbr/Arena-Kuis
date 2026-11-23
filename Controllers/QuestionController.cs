
using ArenaKuis.API.Data;
using ArenaKuis.API.DTOs;
using ArenaKuis.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArenaKuis.API.Controllers;

[ApiController]
[Route("api/questions")]
[Authorize]
public class QuestionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public QuestionsController(AppDbContext db)
    {
        _db = db;
    }

    // =========================
    // GET SEMUA SOAL
    // Hanya Guru dan Admin
    // =========================

    [HttpGet]
    [Authorize(Roles = "Teacher,Admin")]
    public IActionResult GetQuestions(
        string? language,
        string? category)
    {
        var query = _db.Questions.AsQueryable();

        // Guru hanya boleh melihat soal miliknya.
        if (User.IsInRole("Teacher"))
        {
            var userId = AmbilUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            query = query.Where(
                question => question.TeacherId == userId
            );
        }

        if (!string.IsNullOrWhiteSpace(language))
        {
            language = language.Trim();

            query = query.Where(
                question => question.Language == language
            );
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            category = category.Trim();

            query = query.Where(
                question => question.Category == category
            );
        }

        return Ok(query.ToList());
    }

    // =========================
    // CREATE SOAL
    // =========================

    [HttpPost]
    [Authorize(Roles = "Teacher,Admin")]
    public IActionResult CreateQuestion(
        CreateQuestionRequest? request)
    {
        if (request == null)
        {
            return BadRequest("Data soal tidak valid.");
        }

        var error = ValidasiSoal(request);

        if (error != null)
        {
            return BadRequest(error);
        }

        var userId = AmbilUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var question = new Question
        {
            TeacherId = userId.Value,
            Language = request.Language.Trim(),
            Category = request.Category.Trim(),
            QuestionText = request.QuestionText.Trim(),
            OptionA = request.OptionA.Trim(),
            OptionB = request.OptionB.Trim(),
            OptionC = request.OptionC.Trim(),
            OptionD = request.OptionD.Trim(),
            CorrectAnswer = request.CorrectAnswer,
            Difficulty = request.Difficulty.Trim()
        };

        _db.Questions.Add(question);
        _db.SaveChanges();

        return Created(
            $"/api/questions/{question.Id}",
            question
        );
    }

    
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Teacher,Admin")]
    public IActionResult UpdateQuestion(
        int id,
        CreateQuestionRequest? request)
    {
        if (request == null)
        {
            return BadRequest("Data soal tidak valid.");
        }

        var error = ValidasiSoal(request);

        if (error != null)
        {
            return BadRequest(error);
        }

        var question = _db.Questions.Find(id);

        if (question == null)
        {
            return NotFound("Soal tidak ditemukan.");
        }

        // Guru tidak boleh mengubah soal guru lain.
        if (User.IsInRole("Teacher"))
        {
            var userId = AmbilUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            if (question.TeacherId != userId.Value)
            {
                return Forbid();
            }
        }

        question.Language = request.Language.Trim();
        question.Category = request.Category.Trim();
        question.QuestionText = request.QuestionText.Trim();
        question.OptionA = request.OptionA.Trim();
        question.OptionB = request.OptionB.Trim();
        question.OptionC = request.OptionC.Trim();
        question.OptionD = request.OptionD.Trim();
        question.CorrectAnswer = request.CorrectAnswer;
        question.Difficulty = request.Difficulty.Trim();

        _db.SaveChanges();

        return Ok("Soal berhasil diperbarui.");
    }

   
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Teacher,Admin")]
    public IActionResult DeleteQuestion(int id)
    {
        var question = _db.Questions.Find(id);

        if (question == null)
        {
            return NotFound("Soal tidak ditemukan.");
        }

        // Guru tidak boleh menghapus soal guru lain.
        if (User.IsInRole("Teacher"))
        {
            var userId = AmbilUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            if (question.TeacherId != userId.Value)
            {
                return Forbid();
            }
        }

        _db.Questions.Remove(question);
        _db.SaveChanges();

        return Ok("Soal berhasil dihapus.");
    }

    

    private string? ValidasiSoal(
        CreateQuestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Language) ||
            string.IsNullOrWhiteSpace(request.Category) ||
            string.IsNullOrWhiteSpace(request.QuestionText) ||
            string.IsNullOrWhiteSpace(request.OptionA) ||
            string.IsNullOrWhiteSpace(request.OptionB) ||
            string.IsNullOrWhiteSpace(request.OptionC) ||
            string.IsNullOrWhiteSpace(request.OptionD) ||
            string.IsNullOrWhiteSpace(request.Difficulty))
        {
            return "Semua data soal dan pilihan jawaban wajib diisi.";
        }

        if (request.Language.Trim().Length > 50)
        {
            return "Bahasa maksimal 50 karakter.";
        }

        if (request.Category.Trim().Length > 100)
        {
            return "Kategori maksimal 100 karakter.";
        }

        if (request.QuestionText.Trim().Length > 2000)
        {
            return "Soal maksimal 2000 karakter.";
        }

        if (request.OptionA.Trim().Length > 500 ||
            request.OptionB.Trim().Length > 500 ||
            request.OptionC.Trim().Length > 500 ||
            request.OptionD.Trim().Length > 500)
        {
            return "Setiap pilihan jawaban maksimal 500 karakter.";
        }

        if (request.CorrectAnswer < 0 ||
            request.CorrectAnswer > 3)
        {
            return "Indeks jawaban benar harus antara 0 sampai 3.";
        }

        string difficulty = request.Difficulty.Trim();

        if (difficulty != "Easy" &&
            difficulty != "Medium" &&
            difficulty != "Hard")
        {
            return "Difficulty harus Easy, Medium, atau Hard.";
        }

        return null;
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
