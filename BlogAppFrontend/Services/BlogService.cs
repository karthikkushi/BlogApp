using BlogAppFrontend.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Forms;

namespace BlogAppFrontend.Services;

public class BlogService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public BlogService(HttpClient httpClient, AuthService authService, JsonSerializerOptions jsonOptions)
    {
        _httpClient = httpClient;
        _authService = authService;
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            },
            PropertyNamingPolicy = null
        };
        // Remove the IdConverter from outgoing requests to prevent malformed JSON
        // _jsonOptions.Converters.Add(new IdConverter());
    }

    private async Task SetAuthorizationHeaderAsync()
    {
        var token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    // Posts
    public async Task<List<Post>> GetPostsAsync(
        string searchTerm = null,
        string categoryId = null,
        string tag = null,
        string authorId = null,
        string authorName = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        await SetAuthorizationHeaderAsync();

        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrEmpty(searchTerm)) query["searchTerm"] = searchTerm;
        if (!string.IsNullOrEmpty(categoryId)) query["categoryId"] = categoryId;
        if (!string.IsNullOrEmpty(tag)) query["tag"] = tag;
        if (!string.IsNullOrEmpty(authorId)) query["authorId"] = authorId;
        if (!string.IsNullOrEmpty(authorName)) query["authorName"] = authorName;
        if (startDate.HasValue) query["startDate"] = startDate.Value.ToString("O");
        if (endDate.HasValue) query["endDate"] = endDate.Value.ToString("O");

        var uri = query.Count > 0 ? $"posts?{query}" : "posts";
        var response = await _httpClient.GetAsync(uri);
        var rawResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Raw response from /api/{uri}: {rawResponse}");

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to fetch posts. Status code: {response.StatusCode}, Response: {rawResponse}");
        }

        return await response.Content.ReadFromJsonAsync<List<Post>>(_jsonOptions) ?? new List<Post>();
    }

    public async Task<Post> GetPostAsync(string id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.GetAsync($"posts/{id}");
        var rawResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Request to /posts/{id}: Status={response.StatusCode}, Response={rawResponse}");
        response.EnsureSuccessStatusCode();
        var post = await response.Content.ReadFromJsonAsync<Post>(_jsonOptions) ?? throw new Exception($"Post with ID {id} not found.");
        Console.WriteLine($"Parsed post {id} status: {post.Status}");
        return post;
    }

    public async Task<List<Post>> GetMyPostsAsync()
    {
        var userId = await _authService.GetUserIdAsync();
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("User is not logged in.");

        return await GetPostsAsync(authorId: userId);
    }

    public async Task CreatePostAsync(Post post)
    {
        await SetAuthorizationHeaderAsync();

        // Ensure required fields are present
        if (string.IsNullOrEmpty(post.Id))
        {
            post.Id = Guid.NewGuid().ToString();
        }

        var postToSend = new
        {
            Id = post.Id,
            Title = post.Title?.Trim() ?? string.Empty,
            Content = post.Content?.Trim() ?? string.Empty,
            AuthorId = post.AuthorId ?? string.Empty,
            AuthorName = post.AuthorName ?? string.Empty,
            CategoryId = string.IsNullOrEmpty(post.CategoryId) ? null : post.CategoryId,
            Tags = post.Tags?.Where(t => !string.IsNullOrEmpty(t)).ToList() ?? new List<string>(),
            Status = post.Status ?? "Published",
            CreatedAt = post.CreatedAt == default ? DateTime.UtcNow : post.CreatedAt,
            PublishedAt = post.PublishedAt ?? (post.Status == "Published" ? (DateTime?)DateTime.UtcNow : null),
            Reactions = post.Reactions ?? new List<Reaction>()
        };

        Console.WriteLine($"Sending POST to /api/posts: {System.Text.Json.JsonSerializer.Serialize(postToSend)}");
        var response = await _httpClient.PostAsJsonAsync("posts", postToSend);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error response from /api/posts: Status={response.StatusCode}, Content={errorContent}");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await _authService.SignOutAsync();
                throw new HttpRequestException("Unauthorized: Please sign in again.", null, response.StatusCode);
            }

            throw new HttpRequestException(
                $"Failed to create post. Status code: {response.StatusCode}, Response: {errorContent}",
                null,
                response.StatusCode
            );
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Success response from /api/posts: Status={response.StatusCode}, Content={responseContent}");
    }

    public async Task UpdatePostAsync(string id, Post post)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync($"posts/{id}", post);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeletePostAsync(string id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.DeleteAsync($"posts/{id}");
        response.EnsureSuccessStatusCode();
    }

    // Drafts
    public async Task CreateDraftAsync(Post post)
    {
        await SetAuthorizationHeaderAsync();

        // Ensure required fields are present
        if (string.IsNullOrEmpty(post.Id))
        {
            post.Id = Guid.NewGuid().ToString();
        }

        var postToSend = new
        {
            Id = post.Id,
            Title = post.Title?.Trim() ?? string.Empty,
            Content = post.Content?.Trim() ?? string.Empty,
            AuthorId = post.AuthorId ?? string.Empty,
            AuthorName = post.AuthorName ?? string.Empty,
            CategoryId = string.IsNullOrEmpty(post.CategoryId) ? null : post.CategoryId,
            Tags = post.Tags?.Where(t => !string.IsNullOrEmpty(t)).ToList() ?? new List<string>(),
            Status = "Draft",
            CreatedAt = post.CreatedAt == default ? DateTime.UtcNow : post.CreatedAt,
            Reactions = new List<Reaction>()
        };

        var response = await _httpClient.PostAsJsonAsync("posts/draft", postToSend, _jsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error response from /api/posts/draft: Status={response.StatusCode}, Content={errorContent}");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await _authService.SignOutAsync();
                throw new HttpRequestException("Unauthorized: Please sign in again.", null, response.StatusCode);
            }

            try
            {
                var errorObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(errorContent, _jsonOptions);
                if (errorObj?.ContainsKey("errors") == true)
                {
                    var errors = errorObj["errors"].ToString();
                    throw new HttpRequestException($"Failed to create draft: {errors}", null, response.StatusCode);
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Fallback to generic error if JSON parsing fails
            }

            throw new HttpRequestException(
                $"Failed to create draft. Status code: {response.StatusCode}, Response: {errorContent}",
                null,
                response.StatusCode
            );
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Success response from /api/posts/draft: Status={response.StatusCode}, Content={responseContent}");
    }

    public async Task<List<Post>> GetDraftsAsync()
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.GetAsync("posts/drafts");
        response.EnsureSuccessStatusCode();
        var rawResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Raw response from /api/posts/drafts: {rawResponse}");
        var drafts = await response.Content.ReadFromJsonAsync<List<Post>>(_jsonOptions) ?? new List<Post>();

        // Fallback: If Id is null, log and investigate (temporary workaround)
        foreach (var draft in drafts)
        {
            if (string.IsNullOrEmpty(draft.Id))
            {
                Console.WriteLine($"Warning: Draft with null ID detected, Full draft: {System.Text.Json.JsonSerializer.Serialize(draft)}");
                // Temporary fallback: Use a generated ID if needed (commented out for safety)
                // draft.Id = Guid.NewGuid().ToString(); // Uncomment only if backend can handle it
            }
        }

        return drafts;
    }

    private class IdConverter : System.Text.Json.Serialization.JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        break;

                    if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals("_id"))
                    {
                        reader.Read();
                        return reader.TokenType == JsonTokenType.Null ? null : reader.GetString() ?? string.Empty;
                    }
                }
                return null;
            }
            return reader.GetString() ?? string.Empty;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WritePropertyName("_id");
            writer.WriteStringValue(value);
        }
    }

    public async Task UpdateDraftAsync(string id, Post post)
    {
        await SetAuthorizationHeaderAsync();
        
        // Create a clean object for the request to avoid ID serialization issues
        var postToSend = new
        {
            Id = id,
            Title = post.Title?.Trim() ?? string.Empty,
            Content = post.Content?.Trim() ?? string.Empty,
            AuthorId = post.AuthorId ?? string.Empty,
            AuthorName = post.AuthorName ?? string.Empty,
            CategoryId = string.IsNullOrEmpty(post.CategoryId) ? null : post.CategoryId,
            Tags = post.Tags?.Where(t => !string.IsNullOrEmpty(t)).ToList() ?? new List<string>(),
            Status = "Draft",
            CreatedAt = post.CreatedAt,
            PublishedAt = (DateTime?)null
        };
        
        // Log the request for debugging
        Console.WriteLine($"UpdateDraftAsync: Sending PUT request to posts/draft/{id}");
        Console.WriteLine($"UpdateDraftAsync: Post data: {System.Text.Json.JsonSerializer.Serialize(postToSend)}");
        
        var response = await _httpClient.PutAsJsonAsync($"posts/draft/{id}", postToSend, _jsonOptions);
        
        // Log the response for debugging
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"UpdateDraftAsync: Response status: {response.StatusCode}");
        Console.WriteLine($"UpdateDraftAsync: Response content: {responseContent}");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to update draft. Status code: {response.StatusCode}, Response: {responseContent}",
                null,
                response.StatusCode
            );
        }
    }

    public async Task PublishDraftAsync(string id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsync($"posts/draft/{id}/publish", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteDraftAsync(string id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.DeleteAsync($"posts/draft/{id}");
        response.EnsureSuccessStatusCode();
    }

    // Reactions
    public async Task<List<Reaction>> GetReactionsAsync(string postId)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.GetAsync($"posts/{postId}/reactions");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Reaction>>(_jsonOptions) ?? new List<Reaction>();
    }

    public async Task AddReactionAsync(string postId, string reactionType)
    {
        await SetAuthorizationHeaderAsync();
        var request = new ReactionRequest { Type = reactionType };
        var response = await _httpClient.PostAsJsonAsync($"posts/{postId}/react", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveReactionAsync(string postId, string reactionType)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.DeleteAsync($"posts/{postId}/react?reactionType={reactionType}");
        response.EnsureSuccessStatusCode();
    }

    // Comments
    public async Task<List<Comment>> GetCommentsByPostAsync(string postId)
    {
        await SetAuthorizationHeaderAsync();
        var uri = $"comments/post/{postId}";
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
        var response = await _httpClient.SendAsync(requestMessage);
        var rawResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Request to {uri}: Status={response.StatusCode}, Headers={string.Join(", ", response.Headers)}, Response={rawResponse}");
        response.EnsureSuccessStatusCode();
        var comments = await response.Content.ReadFromJsonAsync<List<Comment>>(_jsonOptions) ?? new List<Comment>();
        Console.WriteLine($"Parsed comments for post {postId}: {System.Text.Json.JsonSerializer.Serialize(comments)}");
        return comments;
    }

    public async Task CreateCommentAsync(CommentRequest comment)
    {
        await SetAuthorizationHeaderAsync();
        var uri = "comments";
        Console.WriteLine($"Sending POST to {uri}: {System.Text.Json.JsonSerializer.Serialize(comment)}");
        var response = await _httpClient.PostAsJsonAsync(uri, comment);
        var rawResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response from {uri}: Status={response.StatusCode}, Headers={string.Join(", ", response.Headers)}, Response={rawResponse}");
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateCommentAsync(string id, CommentRequest comment)
    {
        await SetAuthorizationHeaderAsync();
        var uri = $"comments/{id}";
        Console.WriteLine($"Sending PUT to {uri}: {System.Text.Json.JsonSerializer.Serialize(comment)}");
        var response = await _httpClient.PutAsJsonAsync(uri, comment);
        var rawResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response from {uri}: Status={response.StatusCode}, Headers={string.Join(", ", response.Headers)}, Response={rawResponse}");
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteCommentAsync(string id)
    {
        await SetAuthorizationHeaderAsync();
        var uri = $"comments/{id}";
        Console.WriteLine($"Sending DELETE to {uri}");
        var response = await _httpClient.DeleteAsync(uri);
        var rawResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response from {uri}: Status={response.StatusCode}, Headers={string.Join(", ", response.Headers)}, Response={rawResponse}");
        response.EnsureSuccessStatusCode();
    }

    // Categories
    public async Task<List<Category>> GetCategoriesAsync()
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.GetAsync("categories");
        var rawResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Raw response from /api/categories: {rawResponse}");
        response.EnsureSuccessStatusCode();
        var categories = await response.Content.ReadFromJsonAsync<List<Category>>(_jsonOptions) ?? new List<Category>();
        Console.WriteLine($"Parsed categories: {System.Text.Json.JsonSerializer.Serialize(categories)}");
        return categories;
    }

    public async Task<Category> GetCategoryAsync(string id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.GetAsync($"categories/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Category>(_jsonOptions) ?? throw new Exception($"Category with ID {id} not found.");
    }

    public async Task CreateCategoryAsync(Category category)
    {
        await SetAuthorizationHeaderAsync();

        if (string.IsNullOrEmpty(category.Id))
        {
            category.Id = Guid.NewGuid().ToString();
        }

        var categoryToSend = new
        {
            category.Id,
            category.Name,
            category.Description
        };
        var response = await _httpClient.PostAsJsonAsync("categories", categoryToSend);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateCategoryAsync(string id, Category category)
    {
        await SetAuthorizationHeaderAsync();
        
        // Include Id in the payload for backend validation
        var categoryToSend = new
        {
            Id = id,
            Name = category.Name?.Trim() ?? string.Empty,
            Description = category.Description?.Trim() ?? string.Empty
        };
        
        Console.WriteLine($"UpdateCategoryAsync: Sending PUT request to categories/{id}");
        Console.WriteLine($"UpdateCategoryAsync: Category data: {System.Text.Json.JsonSerializer.Serialize(categoryToSend)}");
        
        var response = await _httpClient.PutAsJsonAsync($"categories/{id}", categoryToSend, _jsonOptions);
        
        // Log the response for debugging
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"UpdateCategoryAsync: Response status: {response.StatusCode}");
        Console.WriteLine($"UpdateCategoryAsync: Response content: {responseContent}");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to update category. Status code: {response.StatusCode}, Response: {responseContent}",
                null,
                response.StatusCode
            );
        }
    }

    public async Task DeleteCategoryAsync(string id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.DeleteAsync($"categories/{id}");
        response.EnsureSuccessStatusCode();
    }

    // User Profiles
    public async Task<UserProfile> GetUserProfileAsync(string id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.GetAsync($"userprofiles/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserProfile>(_jsonOptions) ?? throw new Exception($"User profile with ID {id} not found.");
    }

    public async Task<List<Post>> GetUserPostsAsync(string id)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.GetAsync($"userprofiles/{id}/posts");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Post>>(_jsonOptions) ?? new List<Post>();
    }

    public async Task<UserProfile> CreateOrUpdateProfileAsync(UserProfileRequest profile)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("userprofiles", profile);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserProfile>(_jsonOptions)
               ?? throw new Exception("Failed to parse profile response.");
    }

    // Follower Methods
    public async Task FollowUserAsync(string followingId)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("/api/follow/follow", new { FollowingId = followingId }, _jsonOptions);
        response.EnsureSuccessStatusCode();
    }

    public async Task UnfollowUserAsync(string followingId)
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("/api/follow/unfollow", new { FollowingId = followingId }, _jsonOptions);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<Follower>> GetFollowersAsync()
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.GetAsync("/api/follow/followers");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Follower>>(_jsonOptions) ?? new List<Follower>();
    }

    public async Task<List<Follower>> GetFollowingAsync()
    {
        await SetAuthorizationHeaderAsync();
        var response = await _httpClient.GetAsync("/api/follow/following");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Follower>>(_jsonOptions) ?? new List<Follower>();
    }

    // Notification Methods
    public async Task<List<Notification>> GetNotificationsAsync(string query = null)
    {
        await SetAuthorizationHeaderAsync();
        var uri = string.IsNullOrEmpty(query) ? "/api/notification" : $"/api/notification{query}";
        var response = await _httpClient.GetAsync(uri);
        var rawResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Raw response from {uri}: {rawResponse}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Notification>>(_jsonOptions) ?? new List<Notification>();
    }

    public async Task MarkNotificationAsReadAsync(string notificationId)
    {
        await SetAuthorizationHeaderAsync();
        var request = new { NotificationId = notificationId };
        Console.WriteLine($"Sending POST to /api/notification/mark-read with {System.Text.Json.JsonSerializer.Serialize(request)}");
        var response = await _httpClient.PostAsJsonAsync("/api/notification/mark-read", request, _jsonOptions);
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response from /api/notification/mark-read: Status={response.StatusCode}, Content={responseContent}");
        response.EnsureSuccessStatusCode();
    }
    public async Task<List<UserProfile>> GetAllUserProfilesAsync()
    {
        var response = await _httpClient.GetAsync("/api/userprofiles");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<UserProfile>>();
        return result ?? new List<UserProfile>();
    }
}