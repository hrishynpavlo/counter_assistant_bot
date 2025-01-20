using CounterAssistant.Bot.Flows;
using CounterAssistant.Domain.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace CounterAssistant.UnitTests.Bot.Flows
{
    [TestFixture]
    public class CreateCounterFlowTests
    {
        [TestCaseSource(nameof(NotValidUsersTestCases))]
        public void RestoreFromContext_NotValidUser_ThrowsArgumentNullException(User user)
        {
            //ACT
            var act = new TestDelegate(() => CreateCounterFlow.RestoreFromContext(null));

            //ASSERT
            Assert.Throws<ArgumentNullException>(act);
        }

        [TestCaseSource(nameof(NotValidStepTestCases))]
        public void RestoreFromContext_NotValidStep_SuccessTest(User user)
        {
            //ACT
            var flow = CreateCounterFlow.RestoreFromContext(user);

            //ASSERT
            Assert.That(flow, Is.Not.Null);
            Assert.That(CreateFlowSteps.None, Is.EqualTo(flow.State));
        }

        [TestCaseSource(nameof(CounterBuilderTestCases))]
        public void RestoreFromContext_BuilderArgs_SuccessTest(User user, int expectedArgs)
        {
            //ACT
            var flow = CreateCounterFlow.RestoreFromContext(user);

            //ASSERT
            Assert.That(expectedArgs, Is.EqualTo(flow.Args.Count));
        }

        [Test]
        public void Perform_FullFlow_SuccessTest()
        {
            //ARRANGE
            var flow = new CreateCounterFlow();

            //ACT && ASSERT

            //step 1: none
            var result = flow.Perform(string.Empty);
            Assert.That(CreateFlowSteps.SetCounterName, Is.EqualTo(flow.State));
            Assert.That(result.IsCompleted, Is.False);
            Assert.That(string.IsNullOrWhiteSpace(result.Message), Is.False);
            Assert.That(0, Is.EqualTo(flow.Args.Count));

            //step 2: set counter name
            var counterName = "test-counter";
            result = flow.Perform(counterName);
            Assert.That(CreateFlowSteps.SetCounterStep, Is.EqualTo(flow.State));
            Assert.That(result.IsCompleted, Is.False);
            Assert.That(string.IsNullOrWhiteSpace(result.Message), Is.False);
            Assert.That(1, Is.EqualTo(flow.Args.Count));

            //step 3: set counter step
            var counterStep = 1;
            result = flow.Perform(counterStep.ToString());
            Assert.That(CreateFlowSteps.SetCounterType, Is.EqualTo(flow.State));
            Assert.That(result.IsCompleted, Is.False);
            Assert.That(2, Is.EqualTo(flow.Args.Count));
            Assert.That(result.Buttons, Is.Not.Null);

            //step 4: set counter type
            var counterType = CounterType.Automatic;
            result = flow.Perform(counterType.ToString());
            Assert.That(CreateFlowSteps.SetCounterUnit, Is.EqualTo(flow.State));
            Assert.That(result.IsCompleted, Is.False);

            //step 5: set counter unit
            var counterUnit = CounterUnit.Day;
            result = flow.Perform(counterUnit.ToString());
            Assert.That(CreateFlowSteps.Completed, Is.EqualTo(flow.State));
            Assert.That(result.IsCompleted, Is.True);
            Assert.That(string.IsNullOrWhiteSpace(result.Message), Is.False);
            Assert.That(result.Counter, Is.Not.Null);

            Assert.That(counterName, Is.EqualTo(result.Counter.Title));
            Assert.That(counterStep, Is.EqualTo(result.Counter.Step));
            Assert.That(counterUnit, Is.EqualTo(result.Counter.Unit));
        }

        private static IEnumerable<TestCaseData> NotValidUsersTestCases()
        {
            yield return new TestCaseData(null);
            yield return new TestCaseData(new User { BotInfo = null });
        }

        private static IEnumerable<TestCaseData> NotValidStepTestCases()
        {
            yield return new TestCaseData(new User 
            { 
                BotInfo = new UserBotInfo 
                { 
                    CreateCounterFlowInfo = null
                } 
            });

            yield return new TestCaseData(new User 
            {
                BotInfo = new UserBotInfo 
                {
                    CreateCounterFlowInfo = new CreateCounterFlowInfo
                    {
                        State = "asfsaf"
                    }
                }
            });

            yield return new TestCaseData(new User
            {
                BotInfo = new UserBotInfo
                {
                    CreateCounterFlowInfo = new CreateCounterFlowInfo
                    {
                        State = null
                    }
                }
            });
        }

        private static IEnumerable<TestCaseData> CounterBuilderTestCases()
        {
            yield return new TestCaseData(new User
            {
                BotInfo = new UserBotInfo
                {
                    CreateCounterFlowInfo = new CreateCounterFlowInfo 
                    {
                        Args = null
                    }
                }
            }, 0);

            yield return new TestCaseData(new User
            {
                BotInfo = new UserBotInfo
                {
                    CreateCounterFlowInfo = new CreateCounterFlowInfo
                    {
                        Args = new Dictionary<string, object>()
                    }
                }
            }, 0);

            yield return new TestCaseData(new User
            {
                BotInfo = new UserBotInfo
                {
                    CreateCounterFlowInfo = new CreateCounterFlowInfo
                    {
                        Args = new Dictionary<string, object> { ["testttt"] = 0 }
                    }
                }
            }, 0);

            yield return new TestCaseData(new User
            {
                BotInfo = new UserBotInfo
                {
                    CreateCounterFlowInfo = new CreateCounterFlowInfo
                    {
                        Args = new Dictionary<string, object> { ["name"] = "name" }
                    }
                }
            }, 1);

            yield return new TestCaseData(new User
            {
                BotInfo = new UserBotInfo
                {
                    CreateCounterFlowInfo = new CreateCounterFlowInfo
                    {
                        Args = new Dictionary<string, object> { ["step"] = (ushort)1 }
                    }
                }
            }, 1);

            yield return new TestCaseData(new User
            {
                BotInfo = new UserBotInfo
                {
                    CreateCounterFlowInfo = new CreateCounterFlowInfo
                    {
                        Args = new Dictionary<string, object> { ["step"] = (ushort)1, ["name"] = "test" }
                    }
                }
            }, 2);
        }
    }
}
