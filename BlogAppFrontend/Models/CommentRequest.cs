namespace BlogAppFrontend.Models;

public class CommentRequest
{
    public string? PostId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AuthorId { get; set; }
    public string? AuthorName { get; set; }
}