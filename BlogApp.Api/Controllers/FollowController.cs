using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlogApp.Api.Services;
using System.Security.Claims;

namespace BlogApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FollowController : ControllerBase
    {
        private readonly MongoDbService _mongoDbService;
        private readonly Supabase.Client _supabaseClient;

        public FollowController(MongoDbService mongoDbService, Supabase.Client supabaseClient)
        {
            _mongoDbService = mongoDbService ?? throw new ArgumentNullException(nameof(mongoDbService));
            _supabaseClient = supabaseClient ?? throw new ArgumentNullException(nameof(supabaseClient));
        }

        [HttpPost("follow")]
        public async Task<IActionResult> FollowUser([FromBody] FollowRequest request)
        {
            if (string.IsNullOrEmpty(request?.FollowingId))
                return BadRequest("FollowingId is required.");

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not authenticated.");

                if (userId == request.FollowingId)
                    return BadRequest("You cannot follow yourself.");

                var userProfile = await _mongoDbService.GetUserProfileAsync(userId);
                if (userProfile == null)
                    return BadRequest("User profile not found. Please create a profile first.");

                var followingProfile = await _mongoDbService.GetUserProfileAsync(request.FollowingId);
                if (followingProfile == null)
                    return NotFound("User to follow not found.");

                await _mongoDbService.FollowUserAsync(userId, request.FollowingId);
                await _mongoDbService.CreateNotificationAsync(
                    request.FollowingId,
                    "follow",
                    userId,
                    $"{userProfile.Username} started following you"
                );
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FollowUser: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("unfollow")]
        public async Task<IActionResult> UnfollowUser([FromBody] FollowRequest request)
        {
            if (string.IsNullOrEmpty(request?.FollowingId))
                return BadRequest("FollowingId is required.");

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not authenticated.");

                await _mongoDbService.UnfollowUserAsync(userId, request.FollowingId);
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UnfollowUser: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("followers")]
        public async Task<IActionResult> GetFollowers()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not authenticated.");

                var followers = await _mongoDbService.GetFollowersAsync(userId);
                return Ok(followers);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetFollowers: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("following")]
        public async Task<IActionResult> GetFollowing()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not authenticated.");

                var following = await _mongoDbService.GetFollowingAsync(userId);
                return Ok(following);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetFollowing: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    public class FollowRequest
    {
        public string FollowingId { get; set; }
    }
}