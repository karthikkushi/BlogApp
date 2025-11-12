using Microsoft.AspNetCore.Mvc;
using BlogApp.Api.Models;
using BlogApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BlogApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserProfilesController : ControllerBase
    {
        private readonly MongoDbService _mongoDbService;

        public UserProfilesController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService ?? throw new ArgumentNullException(nameof(mongoDbService));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserProfile>> Get(string id)
        {
            try
            {
                var profile = await _mongoDbService.GetUserProfileAsync(id);
                if (profile == null)
                    return NotFound("User profile not found.");
                return profile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Get: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error fetching profile: {ex.Message}");
            }
        }

        [HttpGet("{id}/posts")]
        public async Task<ActionResult<List<Post>>> GetUserPosts(string id)
        {
            try
            {
                var profile = await _mongoDbService.GetUserProfileAsync(id);
                if (profile == null)
                    return NotFound("User profile not found.");

                var posts = await _mongoDbService.GetPostsAsync(authorId: id);
                return posts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserPosts: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error fetching user posts: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdateProfile([FromBody] UserProfile profile)
        {
            if (string.IsNullOrWhiteSpace(profile?.Id) || string.IsNullOrWhiteSpace(profile?.Username))
                return BadRequest("Id and Username are required.");

            var existingProfile = await _mongoDbService.GetUserProfileAsync(profile.Id);

            profile.CreatedAt = existingProfile?.CreatedAt ?? DateTime.UtcNow;
            profile.UpdatedAt = DateTime.UtcNow;

            if (existingProfile == null)
            {
                await _mongoDbService.CreateUserProfileAsync(profile);
                return CreatedAtAction(nameof(Get), new { id = profile.Id }, profile);
            }
            else
            {
                await _mongoDbService.UpdateUserProfileAsync(profile.Id, profile);
                return Ok(profile);
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<UserProfile>>> GetAllUserProfiles()
        {
            try
            {
                var profiles = await _mongoDbService.GetAllUserProfilesAsync();
                return Ok(profiles);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllUserProfiles: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error fetching profiles: {ex.Message}");
            }
        }
    }

    public class UserProfileRequest
    {
        public string Username { get; set; }
        public string Bio { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string CoverPhotoUrl { get; set; }
    }
}