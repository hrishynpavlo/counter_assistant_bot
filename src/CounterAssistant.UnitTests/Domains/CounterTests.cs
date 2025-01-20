using CounterAssistant.Domain.Models;
using NUnit.Framework;

namespace CounterAssistant.UnitTests.Domains
{
    [TestFixture]
    public class CounterTests
    {
        [Test]
        public void Counter_FullFlowTest_SuccessTest()
        {
            //ARRANGE
            var amount = 0;
            ushort step = 10;

            var counter = new Counter("test", amount, step, true, CounterUnit.Time);
            var lastModifiedAtStart = counter.LastModifiedAt;

            //ACT & ASSERT
            Assert.That(lastModifiedAtStart, Is.EqualTo(counter.CreatedAt));
            Assert.That(amount, Is.EqualTo(counter.Amount));
            Assert.That(step, Is.EqualTo(counter.Step));

            counter.Increment();
            counter.Increment();
            Assert.That(counter.LastModifiedAt, Is.EqualTo(lastModifiedAtStart));
            Assert.That(counter.Step * 2 + amount, Is.EqualTo(counter.Amount));

            lastModifiedAtStart = counter.LastModifiedAt;
            var amountBeforeDecrement = counter.Amount;
            counter.Decrement();
            Assert.That(counter.LastModifiedAt, Is.EqualTo(lastModifiedAtStart));
            Assert.That(amountBeforeDecrement - counter.Step, Is.EqualTo(counter.Amount));

            lastModifiedAtStart = counter.LastModifiedAt;
            counter.Reset();
            Assert.That(counter.LastModifiedAt, Is.GreaterThan(lastModifiedAtStart));
            Assert.That(counter.Amount == 0, Is.True);

            var newName = "newName";
            lastModifiedAtStart = counter.LastModifiedAt;
            counter.Rename(newName);
            Assert.That(newName, Is.EqualTo(counter.Title));
            Assert.That(lastModifiedAtStart, Is.EqualTo(counter.LastModifiedAt));
        }
    }
}
