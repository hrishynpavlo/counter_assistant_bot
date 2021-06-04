using CounterAssistant.Domain.Builders;
using NUnit.Framework;

namespace CounterAssistant.UnitTests.Domains
{
    [TestFixture]
    public class CounterBuilderTests
    {
        [TestCase("test1", (ushort)1)]
        [TestCase("test2", (ushort)5)]
        public void CounterBuilder_BuildWithTitleWithStep_SuccessTests(string title, ushort step)
        {
            //ARRANGE
            var builder = CounterBuilder.Default;

            //ACT
            var counter = builder.WithName(title).WithStep(step).Build();

            //ASSERT
            Assert.IsNotNull(counter);
            Assert.AreEqual(title, counter.Title);
            Assert.AreEqual(step, counter.Step);
        }

        [Test]
        public void CounterBuilder_BuildWithNotSetStep_SuccessTest()
        {
            //ARRANGE
            var builder = CounterBuilder.Default;
            var title = "test";

            //ACT
            var counter = builder.WithName(title).Build();

            //ASSERT
            Assert.IsNotNull(counter);
            Assert.AreEqual(CounterBuilder.DefultStep, counter.Step);
        }
    }
}
