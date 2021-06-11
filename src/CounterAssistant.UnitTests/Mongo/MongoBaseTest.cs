using CounterAssistant.DataAccess;
using Microsoft.Extensions.Logging;
using Mongo2Go;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using System;

namespace CounterAssistant.UnitTests.Mongo
{
    [TestFixture]
    public abstract class MongoBaseTest<T> where T: class
    {
        private MongoDbRunner _runner;
        protected IAsyncRepository<T> _repository;

        [OneTimeSetUp]
        protected void BeforeAllTests()
        {
            _runner = MongoDbRunner.Start();
            MongoDefaults.GuidRepresentation = GuidRepresentation.Standard;
            var database = new MongoClient(_runner.ConnectionString).GetDatabase("counter-assistant-tests");
            var collection = database.GetCollection<T>(Guid.NewGuid().ToString());
            var logger = new Mock<ILogger<AsyncRepository<T>>>();

            _repository = new AsyncRepository<T>(collection, logger.Object);
        }

        [OneTimeTearDown]
        protected void AfterAllTests()
        {
            _runner.Dispose();
        }
    }
}
