using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlogApp.Api.Services;
using System.Security.Claims;

namespace BlogApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly MongoDbService _mongoDbService;
        private readonly Supabase.Client _supabaseClient;

        public NotificationController(MongoDbService mongoDbService, Supabase.Client supabaseClient)
        {
            _mongoDbService = mongoDbService ?? throw new ArgumentNullException(nameof(mongoDbService));
            _supabaseClient = supabaseClient ?? throw new ArgumentNullException(nameof(supabaseClient));
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                Console.WriteLine("---- CLAIMS DUMP (before userId check) ----");
                foreach (var claim in User.Claims)
                {
                    Console.WriteLine($"CLAIM TYPE: {claim.Type switch
                    {
                        "sub" => "sub (JWT subject)",
                        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" => "nameidentifier (mapped sub)",
                        _ => claim.Type
                    }}, VALUE: {claim.Value}");
                }
                Console.WriteLine("---- END CLAIMS DUMP ----");
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not authenticated.");

                var notifications = await _mongoDbService.GetNotificationsAsync(userId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching notifications: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error fetching notifications: {ex.Message}");
            }
        }

        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkReadRequest request)
        {
            if (string.IsNullOrEmpty(request?.NotificationId))
                return BadRequest("NotificationId is required.");

            try
            {
                Console.WriteLine("---- CLAIMS DUMP (before userId check) ----");
                foreach (var claim in User.Claims)
                {
                    Console.WriteLine($"CLAIM TYPE: {claim.Type switch
                    {
                        "sub" => "sub (JWT subject)",
                        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" => "nameidentifier (mapped sub)",
                        _ => claim.Type
                    }}, VALUE: {claim.Value}");
                }
                Console.WriteLine("---- END CLAIMS DUMP ----");
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not authenticated.");

                var notification = await _mongoDbService.GetNotificationAsync(request.NotificationId);
                if (notification == null)
                    return NotFound("Notification not found.");

                if (notification.UserId != userId)
                    return Forbid("You can only mark your own notifications as read.");

                await _mongoDbService.MarkNotificationAsReadAsync(request.NotificationId);
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking notification as read: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error marking notification as read: {ex.Message}");
            }
        }
    }

    public class MarkReadRequest
    {
        public string NotificationId { get; set; }
    }
}