using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace TaskManager.Models
{
    public class Comment
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        public string UserId { get; set; } = null!;  // Ko je napisao komentar
        public string TaskId { get; set; } = null!;

        public string Text { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonIgnore]
        public string? AuthorName { get; set; }

    }
}
