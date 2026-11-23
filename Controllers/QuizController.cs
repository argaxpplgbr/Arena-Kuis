
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
    public IActionResult CreateQuiz(CreateQuizRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Judul kuis wajib diisi.");
        }

        if (request.QuestionIds.Count == 0)
        {
            return BadRequest("Kuis harus memiliki soal.");
        }

        var questions = _db.Questions
            .Where(question => request.QuestionIds.Contains(question.Id))
            .ToList();

        if (questions.Count != request.QuestionIds.Count)
        {
            return BadRequest("Ada ID soal yang tidak ditemukan.");
        }

        var teacherId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var quiz = new Quiz
        {
            TeacherId = teacherId,
            Title = request.Title,
            Description = request.Description,
            Language = request.Language,
            DurationMinutes = request.DurationMinutes,
            IsPublished = request.IsPublished,
            QuestionIds = request.QuestionIds
        };

        _db.Quizzes.Add(quiz);

        _db.SaveChanges();

        return Ok(quiz);
    }

    [HttpGet]
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
    [HttpGet("{id}")]
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
            .Where(question => quiz.QuestionIds.Contains(question.Id))
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

        return Ok(new
        {
            quiz.Id,
            quiz.Title,
            quiz.Description,
            quiz.Language,
            quiz.DurationMinutes,
            Questions = questions
        });
    }

    [Authorize(Roles = "Student")]
    [HttpPost("{id}/submit")]
    public IActionResult SubmitQuiz(
        int id,
        SubmitQuizRequest request)
    {
        var quiz = _db.Quizzes.FirstOrDefault(
            quiz => quiz.Id == id && quiz.IsPublished
        );

        if (quiz == null)
        {
            return NotFound("Kuis tidak ditemukan.");
        }

        var studentId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var questions = _db.Questions
            .Where(question => quiz.QuestionIds.Contains(question.Id))
            .ToList();

        if (questions.Count == 0)
        {
            return BadRequest("Kuis tidak memiliki soal.");
        }

        var correct = 0;

        foreach (var question in questions)
        {
            var answer = request.Answers.FirstOrDefault(
                answer => answer.QuestionId == question.Id
            );

            if (answer != null &&
                answer.SelectedAnswer == question.CorrectAnswer)
            {
                correct++;
            }
        }

        var score = (int)Math.Round(
            (double)correct / questions.Count * 100
        );

        var xp = correct * 100;

        var student = _db.Users.Find(studentId);

        if (student == null)
        {
            return Unauthorized();
        }

        student.XP += xp;

        var attempt = new QuizAttempt
        {
            StudentId = studentId,
            QuizId = quiz.Id,
            Score = score,
            CorrectAnswer = correct,
            TotalQuestions = questions.Count,
            XP = xp
        };

        _db.QuizAttempts.Add(attempt);

        _db.SaveChanges();

        return Ok(new
        {
            Score = score,
            CorrectAnswers = correct,
            TotalQuestions = questions.Count,
            XP = xp,
            TotalXP = student.XP
        });
    }
}
