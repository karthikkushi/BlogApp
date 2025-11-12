namespace BlogAppFrontend.Models;

public class Comment
{
    public string Id { get; set; }
    public string PostId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorId { get; set; }
    public string AuthorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}