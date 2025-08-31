using MongoDB.Driver;
using TaskManager.Models;
using TaskManager.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaskManager.Services
{
    public class CommentService
    {
        private readonly IMongoCollection<Comment> _comments;

        public CommentService(MongoDbContext context)
        {
            _comments = context.GetCollection<Comment>("Comments");
        }

        public async Task<List<Comment>> GetAllAsync()
        {
            return await _comments.Find(_ => true).ToListAsync();
        }

        public async Task<List<Comment>> GetByTaskIdAsync(string taskId)
        {
            return await _comments.Find(c => c.TaskId == taskId).ToListAsync();
        }

        public async Task<Comment?> GetByIdAsync(string id)
        {
            return await _comments.Find(c => c.Id == id).FirstOrDefaultAsync();
        }
        public async Task<List<Comment>> GetByUserIdAsync(string userId)
        {
            return await _comments.Find(c => c.UserId == userId).ToListAsync();
        }

        public async Task CreateAsync(Comment comment)
        {
            await _comments.InsertOneAsync(comment);
        }

        public async Task UpdateAsync(string id, Comment updatedComment)
        {
            var filter = Builders<Comment>.Filter.Eq(c => c.Id, id);
            await _comments.ReplaceOneAsync(filter, updatedComment);
        }

        public async Task DeleteAsync(string id)
        {
            await _comments.DeleteOneAsync(c => c.Id == id);
        }
        public async Task DeleteAllByTaskIdAsync(string taskId)
        {
            await _comments.DeleteManyAsync(c => c.TaskId == taskId);
        }
    }

}
