using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CounterAssistant.UnitTests.Mongo
{
    [TestFixture]
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

            //ACT & ASSERT
            var result = await _repository.CreateOneAsync(entity);
            Assert.IsTrue(result);

            var byId = await _repository.FindOneAsync(Builders<TestEntity>.Filter.Eq(x => x.Id, id));
            Assert.IsNotNull(byId);
            Assert.AreEqual(entity, byId);

            var byGuid = await _repository.FindOneAsync(Builders<TestEntity>.Filter.Eq(x => x.GuidField, guid));
            Assert.IsNotNull(byGuid);
            Assert.AreEqual(entity, byGuid);
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
                DateTimeField.Second == other.DateTimeField.Second &&
                DateTimeField.Millisecond == other.DateTimeField.Millisecond;
        }
    }
}
