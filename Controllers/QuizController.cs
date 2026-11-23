
using ArenaKuis.API.Data;
using ArenaKuis.API.DTOs;
using ArenaKuis.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArenaKuis.API.Controllers;

[ApiController]
[Route("api/quizzes")]
public class QuizzesController : ControllerBase
{
    private readonly AppDbContext _db;

    public QuizzesController(AppDbContext db)
    {
        _db = db;
    }

    

    [Authorize(Roles = "Teacher,Admin")]
    [HttpPost]
    public IActionResult CreateQuiz(CreateQuizRequest? request)
    {
        if (request == null)
        {
            return BadRequest("Data kuis tidak valid.");
        }

        string title = request.Title?.Trim() ?? "";
        string description = request.Description?.Trim() ?? "";
        string language = request.Language?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest("Judul kuis wajib diisi.");
        }

        if (title.Length > 150)
        {
            return BadRequest("Judul maksimal 150 karakter.");
        }

        if (description.Length > 2000)
        {
            return BadRequest("Deskripsi maksimal 2000 karakter.");
        }

        if (string.IsNullOrWhiteSpace(language))
        {
            return BadRequest("Bahasa kuis wajib diisi.");
        }

        if (language.Length > 50)
        {
            return BadRequest("Bahasa maksimal 50 karakter.");
        }

        if (request.DurationMinutes < 1 ||
            request.DurationMinutes > 180)
        {
            return BadRequest(
                "Durasi kuis harus antara 1 sampai 180 menit."
            );
        }

        if (request.QuestionIds == null ||
            request.QuestionIds.Count == 0)
        {
            return BadRequest("Kuis harus memiliki soal.");
        }

        if (request.QuestionIds.Count > 100)
        {
            return BadRequest(
                "Kuis maksimal memiliki 100 soal."
            );
        }

        // Cegah ID soal yang sama dimasukkan berkali-kali.
        if (request.QuestionIds.Distinct().Count() !=
            request.QuestionIds.Count)
        {
            return BadRequest(
                "Kuis tidak boleh memiliki ID soal duplikat."
            );
        }

        if (request.QuestionIds.Any(id => id <= 0))
        {
            return BadRequest("ID soal tidak valid.");
        }

        var questions = _db.Questions
            .Where(question =>
                request.QuestionIds.Contains(question.Id))
            .ToList();

        // Pastikan semua ID soal benar-benar ada.
        if (questions.Count != request.QuestionIds.Count)
        {
            return BadRequest(
                "Ada ID soal yang tidak ditemukan."
            );
        }

        var teacherId = AmbilUserId();

        if (teacherId == null)
        {
            return Unauthorized();
        }

        // Guru hanya boleh menggunakan soal miliknya.
        if (User.IsInRole("Teacher") &&
            questions.Any(question =>
                question.TeacherId != teacherId.Value))
        {
            return Forbid();
        }

        var quiz = new Quiz
        {
            TeacherId = teacherId.Value,
            Title = title,
            Description = description,
            Language = language,
            DurationMinutes = request.DurationMinutes,
            IsPublished = request.IsPublished,
            QuestionIds = request.QuestionIds.ToList()
        };

        _db.Quizzes.Add(quiz);
        _db.SaveChanges();

        return Created(
            $"/api/quizzes/{quiz.Id}",
            quiz
        );
    }

    

    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetQuizzes()
    {
        var quizzes = _db.Quizzes
            .Where(quiz => quiz.IsPublished)
            .Select(quiz => new
            {
                quiz.Id,
                quiz.Title,
                quiz.Description,
                quiz.Language,
                quiz.DurationMinutes,
                TotalQuestions = quiz.QuestionIds.Count
            })
            .ToList();

        return Ok(quizzes);
    }

    

    [Authorize]
    [HttpGet("{id:int}")]
    public IActionResult GetQuiz(int id)
    {
        var quiz = _db.Quizzes.FirstOrDefault(
            quiz => quiz.Id == id && quiz.IsPublished
        );

        if (quiz == null)
        {
            return NotFound("Kuis tidak ditemukan.");
        }

        var questions = _db.Questions
            .Where(question =>
                quiz.QuestionIds.Contains(question.Id))
            .Select(question => new QuizQuestionResponse
            {
                Id = question.Id,
                QuestionText = question.QuestionText,
                Options = new List<string>
                {
                    question.OptionA,
                    question.OptionB,
                    question.OptionC,
                    question.OptionD
                }
            })
            .ToList();

        if (questions.Count == 0)
        {
            return BadRequest("Kuis tidak memiliki soal.");
        }

        // CorrectAnswer sengaja tidak dikirim.
        return Ok(new
        {
            quiz.Id,
            quiz.Title,
            quiz.Description,
            quiz.Language,
            quiz.DurationMinutes,
            TotalQuestions = questions.Count,
            Questions = questions
        });
    }

    

    [Authorize(Roles = "Student")]
    [HttpPost("{id:int}/submit")]
    public IActionResult SubmitQuiz(
        int id,
        SubmitQuizRequest? request)
    {
        if (request == null ||
            request.Answers == null)
        {
            return BadRequest("Data jawaban tidak valid.");
        }

        var quiz = _db.Quizzes.FirstOrDefault(
            quiz => quiz.Id == id && quiz.IsPublished
        );

        if (quiz == null)
        {
            return NotFound("Kuis tidak ditemukan.");
        }

        var studentId = AmbilUserId();

        if (studentId == null)
        {
            return Unauthorized();
        }

        var student = _db.Users.Find(studentId.Value);

        if (student == null)
        {
            return Unauthorized();
        }

        // Hilangkan duplikasi ID dari data kuis.
        var questionIds = quiz.QuestionIds
            .Distinct()
            .ToList();

        if (questionIds.Count == 0)
        {
            return BadRequest("Kuis tidak memiliki soal.");
        }

        var questions = _db.Questions
            .Where(question =>
                questionIds.Contains(question.Id))
            .ToList();

        // Pastikan data soal kuis masih lengkap.
        if (questions.Count != questionIds.Count)
        {
            return BadRequest(
                "Data soal kuis tidak lengkap."
            );
        }

        // Jawaban tidak boleh mengandung ID soal
        // di luar kuis yang sedang dikerjakan.
        if (request.Answers.Any(answer =>
            !questionIds.Contains(answer.QuestionId)))
        {
            return BadRequest(
                "Jawaban mengandung ID soal yang tidak valid."
            );
        }

        // Satu soal hanya boleh dijawab sekali.
        if (request.Answers
            .GroupBy(answer => answer.QuestionId)
            .Any(group => group.Count() > 1))
        {
            return BadRequest(
                "Jawaban tidak boleh memiliki soal duplikat."
            );
        }

        // Indeks jawaban harus A-D (0-3).
        if (request.Answers.Any(answer =>
            answer.SelectedAnswer < 0 ||
            answer.SelectedAnswer > 3))
        {
            return BadRequest(
                "Pilihan jawaban harus antara 0 sampai 3."
            );
        }

        int correct = 0;

        foreach (var question in questions)
        {
            var answer = request.Answers.FirstOrDefault(
                answer =>
                    answer.QuestionId == question.Id
            );

            // Soal yang tidak dijawab dianggap salah.
            if (answer != null &&
                answer.SelectedAnswer ==
                question.CorrectAnswer)
            {
                correct++;
            }
        }

        int totalQuestions = questions.Count;

        int score = (int)Math.Round(
            (double)correct / totalQuestions * 100
        );

        // XP hanya diberikan pada percobaan pertama
        // untuk kuis yang sama oleh siswa yang sama.
        bool firstAttempt = !_db.QuizAttempts.Any(
            attempt =>
                attempt.StudentId == studentId.Value &&
                attempt.QuizId == quiz.Id
        );

        int xp = firstAttempt ? correct * 100 : 0;

        student.XP += xp;

        var attempt = new QuizAttempt
        {
            StudentId = studentId.Value,
            QuizId = quiz.Id,
            Score = score,
            CorrectAnswer = correct,
            TotalQuestions = totalQuestions,
            XP = xp
        };

        _db.QuizAttempts.Add(attempt);

        _db.SaveChanges();

        return Ok(new
        {
            Score = score,
            CorrectAnswers = correct,
            TotalQuestions = totalQuestions,
            XP = xp,
            TotalXP = student.XP,
            FirstAttempt = firstAttempt
        });
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
