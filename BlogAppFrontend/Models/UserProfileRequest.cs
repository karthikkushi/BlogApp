using System.ComponentModel.DataAnnotations;

namespace BlogAppFrontend.Models;

public class UserProfileRequest

{
    public string Id { get; set; }
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; }

    public string Bio { get; set; }

    [Url(ErrorMessage = "Please enter a valid URL")]
    public string ProfilePictureUrl { get; set; }
    public string CoverPhotoUrl { get; set; }
}