using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlogApp.Api.Models;
using BlogApp.Api.Services;
using System.Security.Claims;
using MongoDB.Bson;

namespace BlogApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly MongoDbService _mongoDbService;

        public CommentsController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService ?? throw new ArgumentNullException(nameof(mongoDbService));
        }

        [HttpGet("post/{postId}")]
        public async Task<ActionResult<List<Comment>>> GetCommentsByPost(string postId)
        {
            if (string.IsNullOrEmpty(postId))
                return BadRequest("PostId is required.");

            try
            {
                var post = await _mongoDbService.GetPostAsync(postId);
                if (post == null)
                    return NotFound("Post not found.");

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");

                if (post.Status != "Published" && post.AuthorId != userId)
                    return BadRequest("Cannot view comments on a draft post unless you're the author.");

                var comments = await _mongoDbService.GetCommentsByPostAsync(postId);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching comments: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error fetching comments: {ex.Message}");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateComment([FromBody] CommentRequest request)
        {
            if (string.IsNullOrEmpty(request?.PostId) || string.IsNullOrEmpty(request?.Content))
                return BadRequest("PostId and Content are required.");

            try
            {
                var post = await _mongoDbService.GetPostAsync(request.PostId);
                if (post == null)
                    return NotFound("Post not found.");

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not authenticated.");

                var userProfile = await _mongoDbService.GetUserProfileAsync(userId);
                if (userProfile == null)
                    return BadRequest("User profile not found. Please create a profile first.");

                if (post.Status != "Published" && post.AuthorId != userId)
                    return BadRequest("Only authors can comment on draft posts.");

                var comment = new Comment
                {
                    PostId = request.PostId,
                    Content = request.Content,
                    AuthorId = userId,
                    AuthorName = userProfile.Username,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _mongoDbService.CreateCommentAsync(comment);
                if (string.IsNullOrEmpty(comment.Id))
                {
                    Console.WriteLine("Comment insertion failed: ID is null after insertion.");
                    return StatusCode(500, "Failed to create comment: ID was not set.");
                }

                if (post.AuthorId != userId)
                {
                    await _mongoDbService.CreateNotificationAsync(
                        post.AuthorId,
                        "comment",
                        userId,
                        $"{userProfile.Username} commented on your post: {post.Title}",
                        post.Id
                    );
                }

                return CreatedAtAction(nameof(GetCommentsByPost), new { postId = comment.PostId }, comment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating comment: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error creating comment: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateComment(string id, [FromBody] CommentRequest request)
        {
            if (string.IsNullOrEmpty(request?.Content))
                return BadRequest("Content is required.");

            try
            {
                var comment = await _mongoDbService.GetCommentAsync(id);
                if (comment == null)
                    return NotFound("Comment not found.");

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not authenticated.");

                if (comment.AuthorId != userId)
                    return Forbid("You can only edit your own comments.");

                comment.Content = request.Content;
                comment.UpdatedAt = DateTime.UtcNow;

                await _mongoDbService.UpdateCommentAsync(id, comment);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating comment: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error updating comment: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(string id)
        {
            try
            {
                var comment = await _mongoDbService.GetCommentAsync(id);
                if (comment == null)
                    return NotFound("Comment not found.");

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not authenticated.");

                var post = await _mongoDbService.GetPostAsync(comment.PostId);
                if (post == null)
                    return NotFound("Post not found.");

                if (comment.AuthorId != userId && post.AuthorId != userId)
                    return Forbid("You can only delete your own comments or comments on your own posts.");

                await _mongoDbService.DeleteCommentAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting comment: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error deleting comment: {ex.Message}");
            }
        }
    }

    public class CommentRequest
    {
        public string PostId { get; set; }
        public string Content { get; set; }
    }
}