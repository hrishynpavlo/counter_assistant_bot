using CounterAssistant.DataAccess.DTO;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CounterAssistant.DataAccess
{
    public interface ICategoryService
    {
        Task<List<string>> GetCategories();
        Task<string> GetCategoryBySeller(string seller);
        Task AddMatch(string category, string seller);
    }

    public class CategoryService : ICategoryService
    {
        private readonly IMongoCollection<FinancialCategoryDto> _db;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(IMongoCollection<FinancialCategoryDto> db, ILogger<CategoryService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task AddMatch(string category, string seller)
        {
            throw new NotImplementedException();
        }

        public async Task<List<string>> GetCategories()
        {
            return await _db.Find(x => true).Project(x => x.Name).ToListAsync();
        }

        public Task<string> GetCategoryBySeller(string seller)
        {
            throw new NotImplementedException();
        }
    }
}
