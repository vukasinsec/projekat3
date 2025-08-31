using MongoDB.Driver;
using TaskManager.Data;
using TaskManager.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace TaskManager.Services
{
    public class ProjectService
    {
        private readonly IMongoCollection<Project> _projects;
        private readonly IMongoCollection<TaskItem> _tasks;

        public ProjectService(MongoDbContext context)
        {
            _projects = context.GetCollection<Project>("Projects");
            _tasks = context.GetCollection<TaskItem>("Tasks");
        }

        // ========== Projekti ==========

        public async Task<List<Project>> GetAllAsync()
        {
            return await _projects.Find(_ => true).ToListAsync();
        }

        public async Task<Project?> GetByIdAsync(string id)
        {
            return await _projects.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(Project project)
        {
            await _projects.InsertOneAsync(project);
        }

        public async Task UpdateAsync(string id, Project updatedProject)
        {
            await _projects.ReplaceOneAsync(p => p.Id == id, updatedProject);
        }

        public async Task DeleteAsync(string id)
        {
            await _projects.DeleteOneAsync(p => p.Id == id);
        }

        // ========== Kolaboratori ==========

        public async Task AddCollaboratorAsync(string projectId, string userId)
        {
            var project = await _projects.Find(p => p.Id == projectId).FirstOrDefaultAsync();
            if (project == null) throw new Exception("Project not found");

            if (!project.CollaboratorIds.Contains(userId))
            {
                project.CollaboratorIds.Add(userId);
                await UpdateAsync(projectId, project);
            }
        }

        public async Task RemoveCollaboratorAsync(string projectId, string userId)
        {
            var update = Builders<Project>.Update.Pull(p => p.CollaboratorIds, userId);
            await _projects.UpdateOneAsync(p => p.Id == projectId, update);
        }
        public async Task<List<Project>> GetProjectsByOwnerAsync(string ownerId)
        {
            return await _projects.Find(p => p.OwnerId == ownerId).ToListAsync();
        }

        public async Task<List<Project>> GetProjectsByCollaboratorAsync(string userId)
        {
            return await _projects.Find(p => p.CollaboratorIds.Contains(userId)).ToListAsync();
        }


        // ========== Taskovi ==========

        public async Task<List<TaskItem>> GetTasksForProjectAsync(string projectId)
        {
            var filter = Builders<TaskItem>.Filter.Eq(t => t.ProjectId, projectId);
            return await _tasks.Find(filter).ToListAsync();
        }

        public async Task<List<TaskItem>> GetTasksForUserAsync(string userId)
        {
            var userProjects = await _projects.Find(p =>
                p.OwnerId == userId || p.CollaboratorIds.Contains(userId)).ToListAsync();

            var projectIds = userProjects.Select(p => p.Id).ToList();

            var filter = Builders<TaskItem>.Filter.In(t => t.ProjectId, projectIds);
            return await _tasks.Find(filter).ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetTaskStatisticsAsync(string userId)
        {
            var userProjects = await _projects.Find(p =>
                p.OwnerId == userId || p.CollaboratorIds.Contains(userId)).ToListAsync();

            var allTaskIds = userProjects.SelectMany(p => p.TaskIds).ToList();

            var filter = Builders<TaskItem>.Filter.And(
                Builders<TaskItem>.Filter.In(t => t.Id, allTaskIds),
                Builders<TaskItem>.Filter.Eq(t => t.AssignedUserId, userId),
                Builders<TaskItem>.Filter.Eq(t => t.Status, Models.TaskStatus.Done)
            );

            var userTasks = await _tasks.Find(filter).ToListAsync();
            var now = DateTime.UtcNow;

            return new Dictionary<string, int>
            {
                ["Today"] = userTasks.Count(t => t.CompletedAt?.Date == now.Date),
                ["ThisWeek"] = userTasks.Count(t => t.CompletedAt >= now.AddDays(-7)),
                ["ThisMonth"] = userTasks.Count(t => t.CompletedAt >= now.AddMonths(-1)),
                ["ThisYear"] = userTasks.Count(t => t.CompletedAt >= now.AddYears(-1)),
            };
        }

        public async Task<List<Project>> SearchProjectsByNameAsync(string query)
        {
            var filter = Builders<Project>.Filter.Regex("Name", new MongoDB.Bson.BsonRegularExpression(query, "i"));
            return await _projects.Find(filter).ToListAsync();
        }


        public async Task<bool> SendCollaborationRequestAsync(string projectId, string userId)
        {
            var project = await _projects.Find(p => p.Id == projectId).FirstOrDefaultAsync();
            if (project == null || project.PendingCollaboratorIds.Contains(userId) || project.CollaboratorIds.Contains(userId))
                return false;

            var update = Builders<Project>.Update.Push(p => p.PendingCollaboratorIds, userId);
            await _projects.UpdateOneAsync(p => p.Id == projectId, update);
            return true;
        
        }

        public async Task RemovePendingCollaboratorAsync(string projectId, string userId)
        {
            var update = Builders<Project>.Update.Pull(p => p.PendingCollaboratorIds, userId);
            await _projects.UpdateOneAsync(p => p.Id == projectId, update);
        }


        public async Task AddTaskToProjectAsync(string projectId, string taskId)
        {
            var filter = Builders<Project>.Filter.Eq(p => p.Id, projectId);
            var update = Builders<Project>.Update.Push(p => p.TaskIds, taskId);
            await _projects.UpdateOneAsync(filter, update);
        }


    }
}
