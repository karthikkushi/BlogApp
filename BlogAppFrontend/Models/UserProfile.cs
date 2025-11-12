namespace BlogAppFrontend.Models;

public class UserProfile
{
    public string Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; }  // for profile image
    public string CoverPhotoUrl { get; set; }      // for cover image
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}