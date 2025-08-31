using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace TaskManager.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        public string UserName { get; set; } = null!;  // Jedinstveno korisničko ime
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;  // Sažetak lozinke

        public string? ProfileImageUrl { get; set; }
        public string? Bio { get; set; }
        public bool IsAdmin { get; set; } = false;

        // Možemo držati reference na projektove i taskove
        public List<string> ProjectIds { get; set; } = new List<string>();
        public List<string> AssignedTaskIds { get; set; } = new List<string>();

     
    }
}
