namespace BlogAppFrontend.Models;

public class Reaction
{
    public string UserId { get; set; }
    public string Username { get; set; }
    public string Type { get; set; }
    public DateTime ReactedAt { get; set; }
}