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
            Assert.IsNotNull(flow);
            Assert.AreEqual(CreateFlowSteps.None, flow.State);
        }

        [TestCaseSource(nameof(CounterBuilderTestCases))]
        public void RestoreFromContext_BuilderArgs_SuccessTest(User user, int expectedArgs)
        {
            //ACT
            var flow = CreateCounterFlow.RestoreFromContext(user);

            //ASSERT
            Assert.AreEqual(expectedArgs, flow.Args.Count);
        }

        [Test]
        public void Perform_FullFlow_SuccessTest()
        {
            //ARRANGE
            var flow = new CreateCounterFlow();

            //ACT && ASSERT

            //step 1: none
            var result = flow.Perform(string.Empty);
            Assert.AreEqual(CreateFlowSteps.SetCounterName, flow.State);
            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message));
            Assert.AreEqual(0, flow.Args.Count);

            //step2: set counter name
            var counterName = "test-counter";
            result = flow.Perform(counterName);
            Assert.AreEqual(CreateFlowSteps.SetCounterStep, flow.State);
            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message));
            Assert.AreEqual(1, flow.Args.Count);

            //step3: set counter step
            var counterStep = 1;
            result = flow.Perform(counterStep.ToString());
            Assert.AreEqual(CreateFlowSteps.SetCounterType, flow.State);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(2, flow.Args.Count);
            Assert.IsNotNull(result.Buttons);

            //step4: set counter type
            var counterType = CounterType.Automatic;
            result = flow.Perform(counterType.ToString());
            Assert.AreEqual(CreateFlowSteps.Completed, flow.State);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message));
            Assert.IsNotNull(result.Counter);
            Assert.AreEqual(counterName, result.Counter.Title);
            Assert.AreEqual(counterStep, result.Counter.Step);
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
