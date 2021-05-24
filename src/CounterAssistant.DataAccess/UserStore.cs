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
            var cursor = await _db.FindAsync(x => x.Id == id);
            var user = await cursor.FirstOrDefaultAsync();
            return user?.ToDomain();
        }

        public async Task<List<User>> GetUsersById(IEnumerable<int> ids)
        {
            var filter = new FilterDefinitionBuilder<UserDto>().In(x => x.Id, ids);
            var users = await _db.Find(filter).ToListAsync();
            return users.Select(x => x.ToDomain()).ToList();
        }
    }
}
