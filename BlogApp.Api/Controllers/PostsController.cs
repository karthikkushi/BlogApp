using Microsoft.AspNetCore.Mvc;
using BlogApp.Api.Models;
using BlogApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MongoDB.Bson;

namespace BlogApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly MongoDbService _mongoDbService;

        public PostsController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService ?? throw new ArgumentNullException(nameof(mongoDbService));
        }

        [HttpGet]
        public async Task<List<Post>> Get(
            [FromQuery] string searchTerm = null,
            [FromQuery] string categoryId = null,
            [FromQuery] string tag = null,
            [FromQuery] string authorId = null,
            [FromQuery] string authorName = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var requestingUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            Console.WriteLine($"Extracted requestingUserId: {requestingUserId ?? "null"}");
            return await _mongoDbService.GetPostsAsync(searchTerm, categoryId, tag, authorId, authorName, startDate, endDate, requestingUserId);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Post>> Get(string id)
        {
            var post = await _mongoDbService.GetPostAsync(id);
            if (post == null)
            {
                return NotFound(new { Message = $"Post with ID {id} not found." });
            }

            var requestingUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            Console.WriteLine($"Extracted requestingUserId: {requestingUserId ?? "null"}");
            if (post.Status == "Draft" && post.AuthorId != requestingUserId)
            {
                return Forbid("You can only view your own drafts.");
            }

            return post;
        }

        [HttpGet("{id}/reactions")]
        public async Task<ActionResult<List<Reaction>>> GetReactions(string id)
        {
            var post = await _mongoDbService.GetPostAsync(id);
            if (post == null)
            {
                return NotFound(new { Message = $"Post with ID {id} not found." });
            }

            if (post.Status != "Published")
            {
                return BadRequest("Reactions can only be viewed for published posts.");
            }

            var reactions = await _mongoDbService.GetReactionsAsync(id);
            return Ok(reactions);
        }

        [HttpPost("{id}/react")]
        [Authorize]
        public async Task<IActionResult> AddReaction(string id, [FromBody] ReactionRequest request)
        {
            try
            {
                var post = await _mongoDbService.GetPostAsync(id);
                if (post == null)
                {
                    Console.WriteLine($"Post not found for ID: {id}");
                    return NotFound(new { Message = $"Post with ID {id} not found." });
                }

                if (post.Status != "Published")
                {
                    return BadRequest("Reactions can only be added to published posts.");
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                var userProfile = await _mongoDbService.GetUserProfileAsync(userId);
                if (userProfile == null)
                {
                    return BadRequest("User profile not found. Please create a profile first.");
                }

                var validReactionTypes = new List<string> { "Like", "Love", "Wow", "Sad" };
                if (!validReactionTypes.Contains(request.Type))
                {
                    return BadRequest("Invalid reaction type. Allowed types are: Like, Love, Wow, Sad.");
                }

                var reaction = new Reaction
                {
                    UserId = userId,
                    Username = userProfile.Username,
                    Type = request.Type,
                    ReactedAt = DateTime.UtcNow
                };

                await _mongoDbService.AddReactionAsync(id, reaction);
                if (post.AuthorId != userId)
                {
                    await _mongoDbService.CreateNotificationAsync(
                        post.AuthorId,
                        "reaction",
                        userId,
                        $"{userProfile.Username} reacted {request.Type} to your post: {post.Title}",
                        post.Id
                    );
                }

                return Ok(reaction);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding reaction: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error adding reaction: {ex.Message}");
            }
        }

        [HttpDelete("{id}/react")]
        [Authorize]
        public async Task<IActionResult> RemoveReaction(string id, [FromQuery] string reactionType)
        {
            try
            {
                var post = await _mongoDbService.GetPostAsync(id);
                if (post == null)
                {
                    return NotFound(new { Message = $"Post with ID {id} not found." });
                }

                if (post.Status != "Published")
                {
                    return BadRequest("Reactions can only be removed from published posts.");
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                var validReactionTypes = new List<string> { "Like", "Love", "Wow", "Sad" };
                if (!validReactionTypes.Contains(reactionType))
                {
                    return BadRequest("Invalid reaction type. Allowed types are: Like, Love, Wow, Sad.");
                }

                await _mongoDbService.RemoveReactionAsync(id, userId, reactionType);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing reaction: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error removing reaction: {ex.Message}");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post(Post post)
        {
            try
            {
                Console.WriteLine("---- CLAIMS DUMP (before userId check) ----");
                foreach (var claim in User.Claims)
                {
                    Console.WriteLine($"CLAIM TYPE: {claim.Type}, VALUE: {claim.Value}");
                }
                Console.WriteLine("---- END CLAIMS DUMP ----");
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");

                if (!ModelState.IsValid)
                {
                    Console.WriteLine("ModelState errors: " + string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return BadRequest(ModelState);
                }
                Console.WriteLine($"Received post: Title={post.Title}, CategoryId={post.CategoryId ?? "null"}");

                post.Id = null;
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User not authenticated.");
                }

                var userProfile = await _mongoDbService.GetUserProfileAsync(userId);
                var username = userProfile?.Username ?? userEmail;

                post.AuthorId = userId;
                post.AuthorName = username;
                post.Status = "Published";
                post.CreatedAt = DateTime.UtcNow;
                post.PublishedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(post.CategoryId))
                {
                    if (!ObjectId.TryParse(post.CategoryId, out _))
                    {
                        Console.WriteLine($"Invalid CategoryId: {post.CategoryId}");
                        return BadRequest("Invalid CategoryId. It must be a valid 24-character hex string.");
                    }

                    var category = await _mongoDbService.GetCategoryAsync(post.CategoryId);
                    if (category == null)
                    {
                        Console.WriteLine($"Category not found for CategoryId: {post.CategoryId}");
                        return BadRequest("Category not found for the provided CategoryId.");
                    }
                }

                await _mongoDbService.CreatePostAsync(post);
                if (string.IsNullOrEmpty(post.Id))
                {
                    Console.WriteLine("Post insertion failed: ID is null after insertion.");
                    return StatusCode(500, new { Message = "Failed to create post: ID was not set." });
                }

                Console.WriteLine($"Post created successfully with ID: {post.Id}");
                return CreatedAtAction(nameof(Get), new { id = post.Id }, post);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Post endpoint: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "Error creating post.", Details = ex.Message });
            }
        }

        [HttpPost("draft")]
        [Authorize]
        public async Task<IActionResult> CreateDraft([FromBody] Post post)
        {
            if (post == null)
            {
                return BadRequest(new { Message = "Invalid post data provided." });
            }
            try
            {
                Console.WriteLine("---- CLAIMS DUMP (before userId check) ----");
                foreach (var claim in User.Claims)
                {
                    Console.WriteLine($"CLAIM TYPE: {claim.Type}, VALUE: {claim.Value}");
                }
                Console.WriteLine("---- END CLAIMS DUMP ----");
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");

                // Custom validation
                if (string.IsNullOrWhiteSpace(post.Title))
                {
                    ModelState.AddModelError("Title", "Title is required.");
                }
                if (string.IsNullOrWhiteSpace(post.Content))
                {
                    ModelState.AddModelError("Content", "Content is required.");
                }
                if (post.Title != null && (post.Title.Length < 1 || post.Title.Length > 200))
                {
                    ModelState.AddModelError("Title", "Title must be between 1 and 200 characters.");
                }
                if (post.Content != null && (post.Content.Length < 1 || post.Content.Length > 5000))
                {
                    ModelState.AddModelError("Content", "Content must be between 1 and 5000 characters.");
                }
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("Validation errors: " + string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return BadRequest(ModelState);
                }

                post.Id = null;
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
                {
                    Console.WriteLine("Unauthorized: Missing userId or userEmail in token.");
                    return Unauthorized("User not authenticated.");
                }

                var userProfile = await _mongoDbService.GetUserProfileAsync(userId);
                var username = userProfile?.Username ?? userEmail;

                post.AuthorId = userId;
                post.AuthorName = username;
                post.Status = "Draft";
                post.CreatedAt = DateTime.UtcNow;
                post.PublishedAt = null;
                post.Tags = post.Tags ?? new List<string>();
                post.Reactions = post.Reactions ?? new List<Reaction>();

                if (!string.IsNullOrEmpty(post.CategoryId))
                {
                    if (!ObjectId.TryParse(post.CategoryId, out _))
                    {
                        Console.WriteLine($"Invalid CategoryId: {post.CategoryId}");
                        return BadRequest("Invalid CategoryId. It must be a valid 24-character hex string.");
                    }

                    var category = await _mongoDbService.GetCategoryAsync(post.CategoryId);
                    if (category == null)
                    {
                        Console.WriteLine($"Category not found for CategoryId: {post.CategoryId}");
                        return BadRequest("Category not found for the provided CategoryId.");
                    }
                }

                await _mongoDbService.CreatePostAsync(post);
                if (string.IsNullOrEmpty(post.Id))
                {
                    Console.WriteLine("Draft insertion failed: ID is null after insertion.");
                    return StatusCode(500, new { Message = "Failed to create draft: ID was not set." });
                }

                Console.WriteLine($"Draft created successfully with ID: {post.Id}");
                return CreatedAtAction(nameof(Get), new { id = post.Id }, post);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateDraft endpoint: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "Error creating draft.", Details = ex.Message });
            }
        }

        [HttpGet("drafts")]
        [Authorize]
        public async Task<ActionResult<List<Post>>> GetDrafts()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                var drafts = await _mongoDbService.GetDraftsAsync(userId);
                return drafts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDrafts: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error fetching drafts: {ex.Message}");
            }
        }

        [HttpPut("draft/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateDraft(string id, Post post)
        {
            try
            {
                var existingPost = await _mongoDbService.GetPostAsync(id);
                if (existingPost == null)
                {
                    return NotFound(new { Message = $"Draft with ID {id} not found." });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");
                if (existingPost.AuthorId != userId)
                {
                    return Forbid("You can only edit your own drafts.");
                }

                if (existingPost.Status != "Draft")
                {
                    return BadRequest("This post is already published and cannot be edited as a draft.");
                }

                // Validation logic
                if (string.IsNullOrWhiteSpace(post.Title))
                {
                    ModelState.AddModelError("Title", "Title is required.");
                }
                if (string.IsNullOrWhiteSpace(post.Content))
                {
                    ModelState.AddModelError("Content", "Content is required.");
                }
                if (post.Title != null && (post.Title.Length < 1 || post.Title.Length > 200))
                {
                    ModelState.AddModelError("Title", "Title must be between 1 and 200 characters.");
                }
                if (post.Content != null && (post.Content.Length < 1 || post.Content.Length > 5000))
                {
                    ModelState.AddModelError("Content", "Content must be between 1 and 5000 characters.");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (!string.IsNullOrEmpty(post.CategoryId))
                {
                    if (!ObjectId.TryParse(post.CategoryId, out _))
                    {
                        return BadRequest("Invalid CategoryId. It must be a valid 24-character hex string.");
                    }

                    var category = await _mongoDbService.GetCategoryAsync(post.CategoryId);
                    if (category == null)
                    {
                        return BadRequest("Category not found for the provided CategoryId.");
                    }
                }

                post.Id = id;
                post.AuthorId = existingPost.AuthorId;
                post.AuthorName = existingPost.AuthorName;
                post.Status = "Draft";
                post.CreatedAt = existingPost.CreatedAt;
                post.PublishedAt = null;

                await _mongoDbService.UpdatePostAsync(id, post);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateDraft: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error updating draft: {ex.Message}");
            }
        }

        [HttpPost("draft/{id}/publish")]
        [Authorize]
        public async Task<IActionResult> PublishDraft(string id)
        {
            try
            {
                var post = await _mongoDbService.GetPostAsync(id);
                if (post == null)
                {
                    return NotFound(new { Message = $"Draft with ID {id} not found." });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");
                if (post.AuthorId != userId)
                {
                    return Forbid("You can only publish your own drafts.");
                }

                if (post.Status != "Draft")
                {
                    return BadRequest("This post is already published.");
                }

                post.Status = "Published";
                post.PublishedAt = DateTime.UtcNow;

                await _mongoDbService.UpdatePostAsync(id, post);
                return Ok(post);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PublishDraft: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error publishing draft: {ex.Message}");
            }
        }

        [HttpDelete("draft/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteDraft(string id)
        {
            try
            {
                var post = await _mongoDbService.GetPostAsync(id);
                if (post == null)
                {
                    return NotFound(new { Message = $"Draft with ID {id} not found." });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");
                if (post.AuthorId != userId)
                {
                    return Forbid("You can only delete your own drafts.");
                }

                if (post.Status != "Draft")
                {
                    return BadRequest("This post is already published and cannot be deleted as a draft.");
                }

                await _mongoDbService.DeletePostAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteDraft: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error deleting draft: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Put(string id, Post post)
        {
            try
            {
                var existingPost = await _mongoDbService.GetPostAsync(id);
                if (existingPost == null)
                {
                    return NotFound(new { Message = $"Post with ID {id} not found." });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");
                if (existingPost.AuthorId != userId)
                {
                    return Forbid("You can only edit your own posts.");
                }

                if (existingPost.Status != "Published")
                {
                    return BadRequest("This post is a draft. Use the draft update endpoint to edit it.");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (!string.IsNullOrEmpty(post.CategoryId))
                {
                    if (!ObjectId.TryParse(post.CategoryId, out _))
                    {
                        return BadRequest("Invalid CategoryId. It must be a valid 24-character hex string.");
                    }

                    var category = await _mongoDbService.GetCategoryAsync(post.CategoryId);
                    if (category == null)
                    {
                        return BadRequest("Category not found for the provided CategoryId.");
                    }
                }

                post.Id = id;
                post.AuthorId = existingPost.AuthorId;
                post.AuthorName = existingPost.AuthorName;
                post.Status = "Published";
                post.CreatedAt = existingPost.CreatedAt;
                post.PublishedAt = existingPost.PublishedAt;
                post.Reactions = existingPost.Reactions;

                await _mongoDbService.UpdatePostAsync(id, post);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Put: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error updating post: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var post = await _mongoDbService.GetPostAsync(id);
                if (post == null)
                {
                    return NotFound(new { Message = $"Post with ID {id} not found." });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                Console.WriteLine($"Extracted userId: {userId ?? "null"}");
                if (post.AuthorId != userId)
                {
                    return Forbid("You can only delete your own posts.");
                }

                if (post.Status != "Published")
                {
                    return BadRequest("This post is a draft. Use the draft delete endpoint to delete it.");
                }

                await _mongoDbService.DeletePostAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Delete: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Error deleting post: {ex.Message}");
            }
        }
    }

    public class ReactionRequest
    {
        public string Type { get; set; }
    }
}