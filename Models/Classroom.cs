namespace ArenaKuis.API.Models;

public class Classroom
{
    public int Id{get; set;}
    public int TeacherId{get; set;}
    public string Name{get; set;} = "";
    public string ClassCode{get; set;} = "";
    public List<int> StudentsIds{get; set;} = new();
    public DateTime CreatedAt{get; set;} = DateTime.UtcNow;
}