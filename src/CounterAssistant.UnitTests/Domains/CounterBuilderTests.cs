using CounterAssistant.Domain.Builders;
using NUnit.Framework;

namespace CounterAssistant.UnitTests.Domains
{
    [TestFixture]
    public class CounterBuilderTests
    {
        [TestCase("test1", (ushort)1, true)]
        [TestCase("test2", (ushort)5, false)]
        public void CounterBuilder_BuildWithAllFields_SuccessTests(string title, ushort step, bool isManual)
        {
            //ARRANGE
            var builder = CounterBuilder.Default;

            //ACT
            var counter = builder.WithName(title).WithStep(step).WithType(isManual).Build();

            //ASSERT
            Assert.IsNotNull(counter);
            Assert.AreEqual(title, counter.Title);
            Assert.AreEqual(step, counter.Step);
            Assert.AreEqual(isManual, counter.IsManual);
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
