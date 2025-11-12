using MongoDB.Driver;
using BlogApp.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;

namespace BlogApp.Api.Services
{
    public class MongoDbService
    {
        private readonly IMongoCollection<Post> _posts;
        private readonly IMongoCollection<Comment> _comments;
        private readonly IMongoCollection<Category> _categories;
        private readonly IMongoCollection<UserProfile> _userProfiles;
        private readonly IMongoCollection<Follower> _followersCollection;
        private readonly IMongoCollection<Notification> _notificationsCollection;

        public MongoDbService(IConfiguration configuration)
        {
            var client = new MongoClient(configuration["MongoDbSettings:ConnectionString"]);
            var database = client.GetDatabase(configuration["MongoDbSettings:DatabaseName"]);

            // Register BsonClassMap for Post
            BsonClassMap.RegisterClassMap<Post>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });

            // Register BsonClassMap for Follower
            BsonClassMap.RegisterClassMap<Follower>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });

            // Register BsonClassMap for Notification
            BsonClassMap.RegisterClassMap<Notification>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });

            _posts = database.GetCollection<Post>("Posts");
            _comments = database.GetCollection<Comment>("Comments");
            _categories = database.GetCollection<Category>("Categories");
            _userProfiles = database.GetCollection<UserProfile>("UserProfiles");
            _followersCollection = database.GetCollection<Follower>("Followers");
            _notificationsCollection = database.GetCollection<Notification>("Notifications");
        }

        public async Task<List<Post>> GetPostsAsync(
            string searchTerm = null,
            string categoryId = null,
            string tag = null,
            string authorId = null,
            string authorName = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string requestingUserId = null)
        {
            var filter = Builders<Post>.Filter.Empty;

            if (!string.IsNullOrEmpty(requestingUserId))
            {
                filter &= Builders<Post>.Filter.Or(
                    Builders<Post>.Filter.Eq(p => p.Status, "Published"),
                    Builders<Post>.Filter.Where(p => p.Status == "Draft" && p.AuthorId == requestingUserId)
                );
            }
            else
            {
                filter &= Builders<Post>.Filter.Eq(p => p.Status, "Published");
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var titleFilter = Builders<Post>.Filter.Regex(p => p.Title, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"));
                var contentFilter = Builders<Post>.Filter.Regex(p => p.Content, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"));
                filter &= Builders<Post>.Filter.Or(titleFilter, contentFilter);
            }

            if (!string.IsNullOrEmpty(categoryId))
            {
                filter &= Builders<Post>.Filter.Eq(p => p.CategoryId, categoryId);
            }

            if (!string.IsNullOrEmpty(tag))
            {
                filter &= Builders<Post>.Filter.AnyEq(p => p.Tags, tag);
            }

            if (!string.IsNullOrEmpty(authorId))
            {
                filter &= Builders<Post>.Filter.Eq(p => p.AuthorId, authorId);
            }
            if (!string.IsNullOrEmpty(authorName))
            {
                filter &= Builders<Post>.Filter.Regex(p => p.AuthorName, new MongoDB.Bson.BsonRegularExpression(authorName, "i"));
            }

            if (startDate.HasValue)
            {
                filter &= Builders<Post>.Filter.Gte(p => p.CreatedAt, startDate.Value);
            }
            if (endDate.HasValue)
            {
                filter &= Builders<Post>.Filter.Lte(p => p.CreatedAt, endDate.Value);
            }

            return await _posts.Find(filter).ToListAsync();
        }

        public async Task<List<Post>> GetDraftsAsync(string userId)
        {
            var filter = Builders<Post>.Filter.And(
                Builders<Post>.Filter.Eq(p => p.AuthorId, userId),
                Builders<Post>.Filter.Eq(p => p.Status, "Draft")
            );
            return await _posts.Find(filter).ToListAsync();
        }

        public async Task<Post> GetPostAsync(string id) =>
            await _posts.Find(post => post.Id == id).FirstOrDefaultAsync();

        public async Task AddReactionAsync(string postId, Reaction reaction)
        {
            var filter = Builders<Post>.Filter.Eq(p => p.Id, postId);
            var reactionFilter = Builders<Reaction>.Filter.And(
                Builders<Reaction>.Filter.Eq(r => r.UserId, reaction.UserId),
                Builders<Reaction>.Filter.Eq(r => r.Type, reaction.Type)
            );
            var pullUpdate = Builders<Post>.Update.PullFilter(p => p.Reactions, reactionFilter);
            var pushUpdate = Builders<Post>.Update.Push(p => p.Reactions, reaction);

            await _posts.UpdateOneAsync(filter, pullUpdate);
            await _posts.UpdateOneAsync(filter, pushUpdate);
        }

        public async Task RemoveReactionAsync(string postId, string userId, string reactionType)
        {
            var filter = Builders<Post>.Filter.Eq(p => p.Id, postId);
            var reactionFilter = Builders<Reaction>.Filter.And(
                Builders<Reaction>.Filter.Eq(r => r.UserId, userId),
                Builders<Reaction>.Filter.Eq(r => r.Type, reactionType)
            );
            var pullUpdate = Builders<Post>.Update.PullFilter(p => p.Reactions, reactionFilter);

            await _posts.UpdateOneAsync(filter, pullUpdate);
        }

        public async Task<List<Reaction>> GetReactionsAsync(string postId)
        {
            var post = await GetPostAsync(postId);
            return post?.Reactions ?? new List<Reaction>();
        }

        public async Task CreatePostAsync(Post post)
        {
            try
            {
                Console.WriteLine($"Attempting to insert post: Title={post.Title}, AuthorId={post.AuthorId}");
                await _posts.InsertOneAsync(post);
                Console.WriteLine($"Post inserted with ID: {post.Id}");
                if (string.IsNullOrEmpty(post.Id))
                {
                    throw new Exception("Post ID was not set after insertion.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting post: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task UpdatePostAsync(string id, Post post) =>
            await _posts.ReplaceOneAsync(p => p.Id == id, post);

        public async Task DeletePostAsync(string id) =>
            await _posts.DeleteOneAsync(p => p.Id == id);

        public async Task<List<Comment>> GetCommentsByPostAsync(string postId)
        {
            var normalizedPostId = postId.ToLower();
            var filter = Builders<Comment>.Filter.Eq("postId", normalizedPostId);
            return await _comments.Find(filter).ToListAsync();
        }

        public async Task<Comment> GetCommentAsync(string id) =>
            await _comments.Find(comment => comment.Id == id).FirstOrDefaultAsync();

        public async Task CreateCommentAsync(Comment comment)
        {
            comment.PostId = comment.PostId.ToLower(); // Normalize
            await _comments.InsertOneAsync(comment);
        }

        public async Task UpdateCommentAsync(string id, Comment comment) =>
            await _comments.ReplaceOneAsync(c => c.Id == id, comment);

        public async Task DeleteCommentAsync(string id) =>
            await _comments.DeleteOneAsync(c => c.Id == id);

        public async Task<List<Category>> GetCategoriesAsync() =>
            await _categories.Find(category => true).ToListAsync();

        public async Task<Category> GetCategoryAsync(string id) =>
            await _categories.Find(category => category.Id == id).FirstOrDefaultAsync();

        public async Task CreateCategoryAsync(Category category) =>
            await _categories.InsertOneAsync(category);

        public async Task UpdateCategoryAsync(string id, Category category) =>
            await _categories.ReplaceOneAsync(c => c.Id == id, category);

        public async Task DeleteCategoryAsync(string id) =>
            await _categories.DeleteOneAsync(c => c.Id == id);

        public async Task<UserProfile> GetUserProfileAsync(string id) =>
            await _userProfiles.Find(profile => profile.Id == id).FirstOrDefaultAsync();

        public async Task CreateUserProfileAsync(UserProfile profile) =>
            await _userProfiles.InsertOneAsync(profile);

        public async Task UpdateUserProfileAsync(string id, UserProfile profile)
        {
            await _userProfiles.ReplaceOneAsync(p => p.Id == id, profile);
        }

        // Follower methods
        public async Task FollowUserAsync(string followerId, string followingId)
        {
            try
            {
                var follower = new Follower
                {
                    FollowerId = followerId,
                    FollowingId = followingId,
                    CreatedAt = DateTime.UtcNow
                };
                await _followersCollection.InsertOneAsync(follower);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FollowUserAsync: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task UnfollowUserAsync(string followerId, string followingId)
        {
            try
            {
                await _followersCollection.DeleteOneAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UnfollowUserAsync: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task<List<Follower>> GetFollowersAsync(string userId)
        {
            var followers = await _followersCollection
                .Find(f => f.FollowingId == userId)
                .ToListAsync();

            if (followers.Count == 0)
                return followers;

            var followerIds = followers
                .Select(f => f.FollowerId)
                .Distinct()
                .ToList();

            var profiles = await _userProfiles
                .Find(p => followerIds.Contains(p.Id))
                .ToListAsync();

            foreach (var f in followers)
            {
                f.FollowerUsername = profiles
                    .FirstOrDefault(p => p.Id == f.FollowerId)?.Username;
            }

            return followers;
        }

        public async Task<List<Follower>> GetFollowingAsync(string userId)
        {
            var following = await _followersCollection
                .Find(f => f.FollowerId == userId)
                .ToListAsync();

            if (following.Count == 0)
                return following;

            var userIds = following
                .Select(f => f.FollowingId)
                .Distinct()
                .ToList();

            var profiles = await _userProfiles
                .Find(p => userIds.Contains(p.Id))
                .ToListAsync();

            foreach (var f in following)
            {
                f.FollowingUsername = profiles
                .FirstOrDefault(p => p.Id == f.FollowingId)?.Username;
            }

            return following;
        }

        // Notification methods
        public async Task CreateNotificationAsync(string userId, string type, string triggerUserId, string message, string postId = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Type = type,
                    TriggerUserId = triggerUserId,
                    PostId = postId,
                    Message = message,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _notificationsCollection.InsertOneAsync(notification);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateNotificationAsync: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task<List<Notification>> GetNotificationsAsync(string userId)
        {
            return await _notificationsCollection.Find(n => n.UserId == userId).ToListAsync();
        }

        public async Task MarkNotificationAsReadAsync(string notificationId)
        {
            var update = Builders<Notification>.Update.Set(n => n.IsRead, true);
            await _notificationsCollection.UpdateOneAsync(n => n.Id == notificationId, update);
        }

        public async Task<Notification> GetNotificationAsync(string id)
        {
            return await _notificationsCollection.Find(n => n.Id == id).FirstOrDefaultAsync();
        }
        public async Task<List<UserProfile>> GetAllUserProfilesAsync()
        {
            return await _userProfiles
                .Find(_ => true)
                .Project(profile => new UserProfile
                {
                    Id = profile.Id,
                    Username = profile.Username,
                    Bio = profile.Bio,
                    ProfilePictureUrl = profile.ProfilePictureUrl,
                    CoverPhotoUrl = profile.CoverPhotoUrl,
                    CreatedAt = profile.CreatedAt,
                    UpdatedAt = profile.UpdatedAt
                })
                .ToListAsync();
        }
    }
}