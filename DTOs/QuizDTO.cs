
namespace ArenaKuis.API.DTOs;

public class CreateQuizRequest
{
    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    public string Language { get; set; } = "";

    public int DurationMinutes { get; set; }

    public List<int> QuestionIds { get; set; } = new();

    public bool IsPublished { get; set; }
}

public class SubmitQuizRequest
{
    public List<AnswerRequest> Answers { get; set; } = new();
}

public class AnswerRequest
{
    public int QuestionId { get; set; }

    public int SelectedAnswer { get; set; }
}

public class QuizQuestionResponse
{
    public int Id { get; set; }

    public string QuestionText { get; set; } = "";

    public List<string> Options { get; set; } = new();
}
