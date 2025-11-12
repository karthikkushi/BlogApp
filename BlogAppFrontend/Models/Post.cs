namespace BlogAppFrontend.Models;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

public class Post
{
    
    public string Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    public string Content { get; set; } = string.Empty;

    public string AuthorId { get; set; }
    public string AuthorName { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    public string CategoryId { get; set; }

    public List<string> Tags { get; set; } = new List<string>();
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<Reaction> Reactions { get; set; } = new List<Reaction>();
}