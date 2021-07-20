using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CounterAssistant.UnitTests.Mongo
{
    [TestFixture, Category("MongoIntegration")]
    public class AsyncRepositoryTests : MongoBaseTest<TestEntity>
    {
        [Test]
        public async Task CRUD_ValidEntity_SuccessTest()
        {
            //ARRANGE
            var id = 1;
            var guid = Guid.NewGuid();
            var dt = DateTime.UtcNow;

            var entity = new TestEntity 
            {
                Id = id,
                GuidField = guid,
                DateTimeField = dt
            };
            Console.WriteLine($"app: {entity.DateTimeField}");

            //ACT & ASSERT
            var result = await _repository.CreateOneAsync(entity);
            Assert.IsTrue(result);

            var byId = await _repository.FindOneAsync(Builders<TestEntity>.Filter.Eq(x => x.Id, id));
            Console.WriteLine($"db: {byId.DateTimeField}");
            Assert.IsNotNull(byId);
            Assert.AreEqual(entity, byId);

            var byGuid = await _repository.FindOneAsync(Builders<TestEntity>.Filter.Eq(x => x.GuidField, guid));
            Assert.IsNotNull(byGuid);
            Assert.AreEqual(entity, byGuid);
        }

        [Test]
        public void CreateOneAsync_NullEntity_ThrowsArgumentNullException()
        {
            //ACT
            var act = new AsyncTestDelegate(async () => await _repository.CreateOneAsync(null));

            //ASSERT
            Assert.ThrowsAsync<ArgumentNullException>(act);
        }

        [Test]
        public void RemoveOneAsync_NullFilter_ThrowsArgumentNullException()
        {
            //ACT
            var act = new AsyncTestDelegate(async () => await _repository.RemoveOneAsync(null));

            //ASSERT
            Assert.ThrowsAsync<ArgumentNullException>(act);
        }

        [Test]
        public void UpdateManyAsync_NullDescriptor_ThrowsArgumentNullException()
        {
            //ACT
            var act = new AsyncTestDelegate(async () => await _repository.UpdateManyAsync(null));

            //ASSERT
            Assert.ThrowsAsync<ArgumentNullException>(act);
        }

        [TestCaseSource(nameof(NullParameterTestCases))]
        public void UpdateOneAsync_NullParameter_ThrowsArgumentException(FilterDefinition<TestEntity> filter, UpdateDefinition<TestEntity> update)
        {
            //ACT
            var act = new AsyncTestDelegate(async () => await _repository.UpdateOneAsync(filter, update));

            //ASSERT
            Assert.ThrowsAsync<ArgumentNullException>(act);
        }

        private static IEnumerable<TestCaseData> NullParameterTestCases()
        {
            yield return new TestCaseData(null, null);
            yield return new TestCaseData(FilterDefinition<TestEntity>.Empty, null);
        }
    }

    public class TestEntity : IEquatable<TestEntity>
    {
        public int Id { get; set; }

        public Guid GuidField { get; set; }

        public DateTime DateTimeField { get; set; }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, GuidField, DateTimeField);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TestEntity);
        }

        public bool Equals(TestEntity other)
        {
            if (this == null) return false;
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && GuidField == other.GuidField && DateTimeField.Date == other.DateTimeField.Date &&
                DateTimeField.Minute == other.DateTimeField.Minute &&
                DateTimeField.Second == other.DateTimeField.Second;
        }
    }
}
