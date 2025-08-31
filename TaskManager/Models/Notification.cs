using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace TaskManager.Models
{
    public enum NotificationType
    {
        CollaborationRequest,
        CollaborationAccepted,
        CollaborationRejected,
        CollaboratorAdded,
        TaskAssigned,
        CommentAdded,
        Other
    }
    public class Notification
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        public string UserId { get; set; } = null!;  // Komu je namenjena notifikacija
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //razlikujemo ralzicite tipove notifikacije
        public NotificationType Type { get; set; }

        // Za zahtev za saradnju - na kom projektu i koji korisnik šalje zahtev
        public string? ProjectId { get; set; }
        public string? SenderUserId { get; set; }

    }
}
