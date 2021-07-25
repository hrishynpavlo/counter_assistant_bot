using CounterAssistant.DataAccess;
using CounterAssistant.DataAccess.DTO;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CounterAssistant.UnitTests.Mongo
{
    [TestFixture]
    public class CategoryServiceTests : MongoBaseTest<FinancialCategoryDto>
    {
        private ICategoryService _service;
        private IMongoCollection<FinancialCategoryDto> _collection;

        [OneTimeSetUp]
        public void BeforeAllTests()
        {
            _collection = _db.GetCollection<FinancialCategoryDto>(Guid.NewGuid().ToString());

            var seedJson = File.ReadAllText("seed.json");
            var data = JObject.Parse(seedJson)["categories"].ToObject<IEnumerable<FinancialCategoryDto>>();
            _collection.InsertMany(data);

            var logger = new Mock<ILogger<CategoryService>>().Object;
            _service = new CategoryService(_collection, logger);
        }

        [Test]
        public async Task GetCategories_SuccessTest()
        {
            var result = await _service.GetCategories();
            await _service.AddMatch("Grocery", "NOVUS");
            await _service.AddMatch("Grocery", "NOVUS");
            await _service.AddMatch("Grocery13", "NOVUS");
            await _service.AddMatch("Grocery13", "N4OVUS");
            result = await _service.GetCategories();

            var all = await _collection.Find(x => true).ToListAsync();

            var test = await _service.GetCategoryBySeller("NOVUS");
            var t2 = await _service.GetCategoryBySeller("xyz");
        }
    }
}
