using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
namespace TaskManager.Models
{
    public enum TaskStatus
    {
        ToDo,
        InProgress,
        Done
    }

    public enum TaskPriority
    {
        Low,
        Medium,
        High
    }

    public class TaskItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
      
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public DateTime? DueDate { get; set; }

        public TaskStatus Status { get; set; } = TaskStatus.ToDo;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public List<Comment> Comments { get; set; } = new List<Comment>();

        public DateTime? CompletedAt { get; set; }

        [BindNever]
        public string? AssignedUserId { get; set; }

        [BindNever]
        public string CreatedByUserId { get; set; } = null!;

        [Required]
        public string ProjectId { get; set; } = null!;



    }
}
