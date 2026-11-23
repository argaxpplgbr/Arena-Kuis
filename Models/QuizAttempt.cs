namespace ArenaKuis.API.Models;

public class QuizAttempt
{
    public int Id{get; set;}
    public int StudentId{get; set;}
    public int QuizId{get; set;}
    public int Score{get; set;}
    public int CorrectAnswer{get; set;}
    public int TotalQuestions{get; set;}
    public int XP{get; set;}
    public DateTime CompletedAt{get; set;} = DateTime.UtcNow;
    
}