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

            var counter = new Counter("test", amount, step, true);
            var lastModifiedAtStart = counter.LastModifiedAt;

            //ACT & ASSERT
            Assert.AreEqual(lastModifiedAtStart, counter.CreatedAt);
            Assert.AreEqual(amount, counter.Amount);
            Assert.AreEqual(step, counter.Step);

            counter.Increment();
            counter.Increment();
            Assert.Greater(counter.LastModifiedAt, lastModifiedAtStart);
            Assert.AreEqual(counter.Step * 2 + amount, counter.Amount);

            lastModifiedAtStart = counter.LastModifiedAt;
            var amountBeforeDecrement = counter.Amount;
            counter.Decrement();
            Assert.Greater(counter.LastModifiedAt, lastModifiedAtStart);
            Assert.AreEqual(amountBeforeDecrement - counter.Step, counter.Amount);

            lastModifiedAtStart = counter.LastModifiedAt;
            counter.Reset();
            Assert.Greater(counter.LastModifiedAt, lastModifiedAtStart);
            Assert.IsTrue(counter.Amount == 0);
        }
    }
}
