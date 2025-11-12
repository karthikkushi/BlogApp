using System.Collections.Generic;
using System.Threading.Tasks;
using BlogAppFrontend.Models;

namespace BlogAppFrontend.Services
{
    public interface IBlogService
    {
        Task<UserProfile> GetUserProfileAsync(string userId);
        Task<List<Post>> GetUserPostsAsync(string userId);
        Task CreateOrUpdateProfileAsync(UserProfileRequest profile);
        Task<Post> GetPostAsync(string postId);
        Task<List<Post>> GetAllPostsAsync();
        Task CreatePostAsync(Post post);
        Task UpdatePostAsync(Post post);
        Task DeletePostAsync(string postId);
    }
} 