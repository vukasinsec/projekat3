using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace TaskManager.Models
{
    public class Project
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string OwnerId { get; set; } = null!;  // Ko je vlasnik projekta (User.Id)

        public List<string> TaskIds { get; set; } = new List<string>();
        public List<string> CollaboratorIds { get; set; } = new List<string>();

        public List<string> PendingCollaboratorIds { get; set; } = new();

    }
}
