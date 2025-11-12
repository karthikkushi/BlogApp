using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace BlogApp.Api.Models
{
    public class Notification
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("userId")]
        [Required]
        public string UserId { get; set; } // Supabase user ID (recipient)

        [BsonElement("type")]
        [Required]
        public string Type { get; set; } // e.g., "follow"

        [BsonElement("triggerUserId")]
        [Required]
        public string TriggerUserId { get; set; } // Supabase user ID (trigger)

        [BsonElement("postId")]
        public string PostId { get; set; } // Optional

        [BsonElement("message")]
        [Required]
        public string Message { get; set; }

        [BsonElement("isRead")]
        public bool IsRead { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}