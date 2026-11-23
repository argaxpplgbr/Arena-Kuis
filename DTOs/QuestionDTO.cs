
namespace ArenaKuis.API.DTOs;

public class CreateQuestionRequest
{
    public string Language { get; set; } = "";

    public string Category { get; set; } = "";

    public string QuestionText { get; set; } = "";

    public string OptionA { get; set; } = "";

    public string OptionB { get; set; } = "";

    public string OptionC { get; set; } = "";

    public string OptionD { get; set; } = "";

    public int CorrectAnswer { get; set; }

    public string Difficulty { get; set; } = "Easy";
}
