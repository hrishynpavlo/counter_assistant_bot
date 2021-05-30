using CounterAssistant.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CounterAssistant.DataAccess
{
    public interface IUserStore
    {
        Task<User> GetUserAsync(int id);
        Task CreateUserAsync(User user);
        Task<List<User>> GetUsersById(IEnumerable<int> ids);

        Task UpdateUserAsync(User user);
    }
}
