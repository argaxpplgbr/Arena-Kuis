namespace ArenaKuis.API.Models;

public class Quiz
{
    public int Id{get; set;}
    public int TeacherId{get; set;}
    public string Title{get; set;} = "";
    public string Description{get; set;} = "";
    public string Language{get; set;} = "";
    public int DurationMinutes{get; set;} = 15;
    public bool IsPublished{get; set;} 
    public List<int> QuestionIds{get; set;} = new();
    public DateTime CreatedAt{get; set;} = DateTime.UtcNow;
}