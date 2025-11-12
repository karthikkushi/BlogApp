using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BlogApp.Api.Models
{
    public class Post
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required.")]
        [StringLength(5000, MinimumLength = 1, ErrorMessage = "Content must be between 1 and 5000 characters.")]
        public string Content { get; set; } = string.Empty;

        public string AuthorId { get; set; }

        public string AuthorName { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string CategoryId { get; set; }

        public List<string> Tags { get; set; } = new List<string>();

        public string Status { get; set; } = "Draft";

        public DateTime CreatedAt { get; set; }

        public DateTime? PublishedAt { get; set; }

        public List<Reaction> Reactions { get; set; } = new List<Reaction>();
    }
}