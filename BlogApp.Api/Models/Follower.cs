using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace BlogApp.Api.Models
{
    public class Follower
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("followerId")]
        [Required]
        public string FollowerId { get; set; }

        [BsonElement("followingId")]
        [Required]
        public string FollowingId { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("followerUsername")]
        public string FollowerUsername { get; set; }

        [BsonElement("followingUsername")]
        public string FollowingUsername { get; set; }
    }
}