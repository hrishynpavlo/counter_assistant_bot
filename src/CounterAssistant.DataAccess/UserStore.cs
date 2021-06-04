using CounterAssistant.DataAccess.DTO;
using CounterAssistant.Domain.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CounterAssistant.DataAccess
{
    public class UserStore : IUserStore
    {
        private readonly IMongoCollection<UserDto> _db;
        private readonly ILogger<UserStore> _logger;

        public UserStore(IMongoCollection<UserDto> db, ILogger<UserStore> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task CreateUserAsync(User user)
        {
            await _db.InsertOneAsync(UserDto.FromDomain(user));
        }

        public async Task<User> GetUserAsync(int id)
        {
            var user = await _db.Find(x => x.Id == id).FirstOrDefaultAsync();

            if(user == null)
            {
                _logger.LogWarning("user {id} wasn't found", id);
            }

            return user?.ToDomain();
        }

        public async Task<List<User>> GetUsersById(IEnumerable<int> ids)
        {
            var filter = Builders<UserDto>.Filter.In(x => x.Id, ids);
            var users = await _db.Find(filter).ToListAsync();
            return users.Select(x => x.ToDomain()).ToList();
        }

        public async Task UpdateUserAsync(User user)
        {
            var update = Builders<UserDto>.Update
                .Set(x => x.BotInfo.CreateCounterFlowInfo, user.BotInfo.CreateCounterFlowInfo)
                .Set(x => x.BotInfo.LastCommand, user.BotInfo.LastCommand)
                .Set(x => x.BotInfo.SelectedCounterId, user.BotInfo.SelectedCounterId);

            await _db.FindOneAndUpdateAsync(x => x.Id == user.TelegramId, update);
        }
    }
}
