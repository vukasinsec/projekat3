using MongoDB.Driver;
using TaskManager.Models;
using TaskManager.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace TaskManager.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;
        private readonly ProjectService _projectService;

        public UserService(MongoDbContext context, ProjectService projectService)
        {
            _users = context.GetCollection<User>("Users");
            _projectService = projectService;
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _users.Find(_ => true).ToListAsync();
        }

        public async Task<User?> GetByIdAsync(string id)
        {
            return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(User user)
        {
            await _users.InsertOneAsync(user);
        }

        public async Task UpdateAsync(string id, User updatedUser)
        {
            await _users.ReplaceOneAsync(u => u.Id == id, updatedUser);
        }

        public async Task DeleteAsync(string id)
        {
            await _users.DeleteOneAsync(u => u.Id == id);
        }



        public async Task<List<User>> GetCollaboratorsForUserAsync(string userId)
        {
            var projects = await _projectService.GetAllAsync();
            var userProjects = projects.Where(p => p.OwnerId == userId || p.CollaboratorIds.Contains(userId)).ToList();

            var collaboratorIds = new HashSet<string>();

            foreach (var project in userProjects)
            {
                if (project.OwnerId != userId)
                    collaboratorIds.Add(project.OwnerId);

                foreach (var colId in project.CollaboratorIds)
                {
                    if (colId != userId)
                        collaboratorIds.Add(colId);
                }
            }

            var filter = Builders<User>.Filter.In(u => u.Id, collaboratorIds.ToList());
            return await _users.Find(filter).ToListAsync();
        }


    }

}
