using MongoDB.Driver;
using TaskManager.Models;
using TaskManager.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaskManager.Services
{
    public class TaskService
    {
        private readonly IMongoCollection<TaskItem> _tasks;
        private readonly CommentService _commentService;
        private readonly ProjectService _projectService;
        public TaskService(MongoDbContext context, CommentService commentService, ProjectService projectService)
        {
            _tasks = context.GetCollection<TaskItem>("Tasks");
            _commentService = commentService;
            _projectService = projectService;
        }

        public async Task<List<TaskItem>> GetAllAsync()
        {
            return await _tasks.Find(_ => true).ToListAsync();
        }

        public async Task<List<TaskItem>> GetByProjectIdAsync(string projectId)
        {
            return await _tasks.Find(t => t.ProjectId == projectId).ToListAsync();
        }

        public async Task<TaskItem?> GetByIdAsync(string id)
        {
            return await _tasks.Find(t => t.Id == id).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(TaskItem task)
        {
            await _tasks.InsertOneAsync(task);
        }

        public async Task UpdateAsync(string id, TaskItem updatedTask)
        {
            var update = Builders<TaskItem>.Update
                .Set(t => t.Title, updatedTask.Title)
                .Set(t => t.Description, updatedTask.Description)
                .Set(t => t.DueDate, updatedTask.DueDate)
                .Set(t => t.Status, updatedTask.Status)
                .Set(t => t.Priority, updatedTask.Priority)
                .Set(t => t.AssignedUserId, updatedTask.AssignedUserId); // ← OVO JE KLJUČNO

            await _tasks.UpdateOneAsync(t => t.Id == id, update);
        }

        public async Task DeleteAsync(string id)
        {
            await _tasks.DeleteOneAsync(t => t.Id == id);
        }

        public async Task UpdateStatusAsync(string taskId, Models.TaskStatus newStatus)
        {
            var update = Builders<TaskItem>.Update.Set(t => t.Status, newStatus);
            await _tasks.UpdateOneAsync(t => t.Id == taskId, update);
        }

        public async Task UpdatePriorityAsync(string taskId, TaskPriority newPriority)
        {
            var update = Builders<TaskItem>.Update.Set(t => t.Priority, newPriority);
            await _tasks.UpdateOneAsync(t => t.Id == taskId, update);
        }

        public async Task AssignUserAsync(string taskId, string userId)
        {
            var update = Builders<TaskItem>.Update.Set(t => t.AssignedUserId, userId);
            await _tasks.UpdateOneAsync(t => t.Id == taskId, update);
        }

        public async Task<List<Comment>> GetCommentsForTaskAsync(string taskId)
        {
            return await _commentService.GetByTaskIdAsync(taskId);
        }

        // Ako želiš da komentare držiš u zasebnoj kolekciji, koristi ovu metodu za dodavanje:
        public async Task AddCommentAsync(Comment comment)
        {
            await _commentService.CreateAsync(comment);
        }

        // Ako baš hoćeš da dodaš komentar direktno u listu u TaskItem (nije preporučljivo na duže staze):
        public async Task AddCommentToTaskItemAsync(string taskId, Comment comment)
        {
            var update = Builders<TaskItem>.Update.Push(t => t.Comments, comment);
            await _tasks.UpdateOneAsync(t => t.Id == taskId, update);
        }

        public async Task<List<TaskItem>> FilterAsync(string projectId, Models.TaskStatus? status = null, string? userId = null)
        {
            var filterBuilder = Builders<TaskItem>.Filter;
            var filter = filterBuilder.Eq(t => t.ProjectId, projectId);

            if (status.HasValue)
            {
                filter &= filterBuilder.Eq(t => t.Status, status.Value);
            }

            if (!string.IsNullOrEmpty(userId))
            {
                filter &= filterBuilder.Eq(t => t.AssignedUserId, userId);
            }

            return await _tasks.Find(filter).ToListAsync();
        }

        public async Task<Project?> GetProjectByTaskIdAsync(string taskId)
        {
            var task = await _tasks.Find(t => t.Id == taskId).FirstOrDefaultAsync();
            if (task == null)
                return null;

            return await _projectService.GetByIdAsync(task.ProjectId);
        }

        public async Task<Dictionary<string, int>> GetTaskStatusCountsForUserAsync(string userId)
        {
            var tasks = await _tasks.Find(t => t.AssignedUserId == userId).ToListAsync();

            return tasks
                .GroupBy(t => t.Status.ToString()) // Status kao string: "ToDo", "InProgress", "Done"
                .ToDictionary(g => g.Key, g => g.Count());
        }


    }
}
