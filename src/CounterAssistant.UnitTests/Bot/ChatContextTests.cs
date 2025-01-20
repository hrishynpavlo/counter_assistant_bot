using CounterAssistant.Bot;
using CounterAssistant.Bot.Flows;
using CounterAssistant.Domain.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace CounterAssistant.UnitTests.Bot
{
    [TestFixture]
    public class ChatContextTests
    {
        [TestCase("", TestName = "empty string")]
        [TestCase("             ", TestName = "whitespace string")]
        [TestCase(null, TestName = "null string")]
        public void SetCurrentCommand_InvalidParam_ThrowsArgumentNullException(string command)
        {
            //ARRANGE
            var context = new ChatContext();

            //ACT
            var act = new TestDelegate(() => context.SetCurrentCommand(command));

            //ASSERT
            Assert.Throws<ArgumentNullException>(act);
        }

        [Test]
        public void SelectCounter_NullCounter_ThrowsArgumentNullException()
        {
            //ARRNAGE
            var context = new ChatContext();

            //ACT
            var act = new TestDelegate(() => context.SelectCounter(null));

            //ASSERT
            Assert.Throws<ArgumentNullException>(act);
        }

        [TestCaseSource(nameof(RestoreInvalidParamsCases))]
        public void Restore_IvalidParams_ThrowsArgumentNullException(User user)
        {
            //ACT
            var act = new TestDelegate(() => ChatContext.Restore(user, null));

            //ASSERT
            Assert.Throws<ArgumentNullException>(act);
        }

        [Test]
        public void Restore_WithoutParams_SuccessTest()
        {
            //ARRANGE
            var user = new User
            {
                BotInfo = new UserBotInfo
                {
                    ChatId = 2000,
                    UserName = "@test",
                    LastCommand = BotCommands.START_COMMAND
                },
                TelegramId = 1000,
                FirstName = "John",
                LastName = "Doe"
            };

            //ACT
            var context = ChatContext.Restore(user, null);

            //ASSERT
            Assert.That(context, Is.Not.Null);
            Assert.That(user.TelegramId, Is.EqualTo(context.UserId));
            Assert.That(user.BotInfo.ChatId, Is.EqualTo(context.ChatId));
            Assert.That(user.BotInfo.UserName, Is.EqualTo(context.UserName));
            Assert.That($"{user.FirstName} {user.LastName}", Is.EqualTo(context.Name));
            Assert.That(user.BotInfo.LastCommand, Is.EqualTo(context.Command));
            Assert.That(user.BotInfo.CreateCounterFlowInfo, Is.Null);
            Assert.That(user.BotInfo.SelectedCounterId, Is.Null);
        }

        [Test]
        public void Restore_WithParams_SuccessTest()
        {
            //ARRANGE
            var counterId = Guid.NewGuid();

            var user = new User
            {
                TelegramId = 1001,
                BotInfo = new UserBotInfo
                {
                    ChatId = 10001,
                    LastCommand = BotCommands.CREATE_COUNTER_COMMAND,
                    SelectedCounterId = counterId,
                    CreateCounterFlowInfo = new CreateCounterFlowInfo
                    {
                        State = "None",
                        Args = new Dictionary<string, object>()
                    }
                }
            };

            var counter = new Counter(counterId, nameof(Restore_WithParams_SuccessTest), 1, 1, DateTime.UtcNow, null, true, CounterUnit.Time);

            //ACT
            var context = ChatContext.Restore(user, counter);

            //ASSERT
            Assert.That(context, Is.Not.Null);
            Assert.That(context.SelectedCounter, Is.Not.Null);
            Assert.That(context.CreateCounterFlow, Is.Not.Null);
            Assert.That(counter.Id, Is.EqualTo(context.SelectedCounter.Id));
            Assert.That(user.BotInfo.CreateCounterFlowInfo.Args.Count, Is.EqualTo(context.CreateCounterFlow.Args.Count));
            Assert.That(Enum.Parse<CreateFlowSteps>(user.BotInfo.CreateCounterFlowInfo.State, true), Is.EqualTo(context.CreateCounterFlow.State));
        }

        private static IEnumerable<TestCaseData> RestoreInvalidParamsCases()
        {
            yield return new TestCaseData(null);
            yield return new TestCaseData(new User());
        }
    }
}
