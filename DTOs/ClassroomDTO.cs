
namespace ArenaKuis.API.DTOs;

public class CreateClassroomRequest
{
    public string Name { get; set; } = "";
}

public class JoinClassroomRequest
{
    public string ClassCode { get; set; } = "";
}
