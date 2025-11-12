namespace BlogApp.Api.Models
{
    public class Reaction
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Type { get; set; } // e.g., "Like", "Love", "Wow", "Sad"
        public DateTime ReactedAt { get; set; }
    }
}