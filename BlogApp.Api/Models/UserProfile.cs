using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BlogApp.Api.Models
{
    public class UserProfile
    {
        [BsonId]
        public string Id { get; set; } // Matches the Supabase user ID (not an ObjectId)

        public string Username { get; set; } = string.Empty;

        public string Bio { get; set; } = string.Empty;

        public string ProfilePictureUrl { get; set; } = string.Empty;
        public string CoverPhotoUrl { get; set; } = string.Empty;


        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}