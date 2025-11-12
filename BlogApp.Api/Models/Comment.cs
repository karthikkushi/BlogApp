using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Comment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("postId")]
    [BsonRepresentation(BsonType.String)]
    public string PostId { get; set; }

    [BsonElement("content")]
    public string Content { get; set; }

    [BsonElement("authorId")]
    public string AuthorId { get; set; }

    [BsonElement("authorName")]
    public string AuthorName { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
