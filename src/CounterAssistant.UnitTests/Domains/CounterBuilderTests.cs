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
            Assert.That(counter, Is.Not.Null);
            Assert.That(title, Is.EqualTo(counter.Title));
            Assert.That(step, Is.EqualTo(counter.Step));
            Assert.That(isManual, Is.EqualTo(counter.IsManual));
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
            Assert.That(counter, Is.Not.Null);
            Assert.That(CounterBuilder.DefultStep, Is.EqualTo(counter.Step));
        }
    }
}
