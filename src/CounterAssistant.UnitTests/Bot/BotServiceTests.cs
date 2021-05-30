using App.Metrics;
using App.Metrics.Counter;
using CounterAssistant.Bot;
using CounterAssistant.Bot.Flows;
using CounterAssistant.DataAccess;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CounterAssistant.UnitTests.Bot
{
    [TestFixture]
    public class BotServiceTests
    {
        private Mock<IMetricsRoot> _metrics;
        private Mock<IContextProvider> _provider;
        private BotService _bot;

        [SetUp]
        public void Init()
        {
            var counter = new Mock<IMeasureCounterMetrics>();
            var measure = new Mock<IMeasureMetrics>();
            measure.Setup(x => x.Counter).Returns(counter.Object);
            _metrics = new Mock<IMetricsRoot>();
            _metrics.Setup(x => x.Measure).Returns(measure.Object);

            var counterStore = new Mock<ICounterStore>();
            counterStore.Setup(x => x.GetCountersByUserIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Domain.Models.Counter>());
            counterStore.Setup(x => x.GetCounterByNameAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new Domain.Models.Counter("test", 1, 1, true));

            var logger = new Mock<ILogger<BotService>>();
            var botClient = new Mock<ITelegramBotClient>();

            _provider = new Mock<IContextProvider>();
            _bot = new BotService(botClient.Object, _provider.Object, counterStore.Object, logger.Object, _metrics.Object);
        }

        [Test]
        public void HandleMessage_NotCommand_SuccessTest()
        {
            //ARRANGE
            _provider.Setup(x => x.GetContextAsync(It.IsAny<Message>())).ReturnsAsync(new ChatContext { });

            var message = CreateMessage(text: "lores ipsum");

            //ACT
            var act = new AsyncTestDelegate(async() => await _bot.HandleMessage(message));

            //ASSERT
            Assert.DoesNotThrowAsync(act);
        }

        [Test]
        public async Task HandleMessage_StartCommand_SuccessTest()
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1,
            };

            _provider.Setup(x => x.GetContextAsync(It.IsAny<Message>())).ReturnsAsync(context);

            //ACT
            await _bot.HandleMessage(CreateMessage(BotCommands.START_COMMAND));

            //ASSERT
            Assert.AreEqual(BotCommands.START_COMMAND, context.Command);
        }

        [Test]
        public async Task HandleMessage_CreateCounterFullFlow_SuccessTest()
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1,
            };

            _provider.Setup(x => x.GetContextAsync(It.IsAny<Message>())).ReturnsAsync(context);

            //ACT && ASSERT

            //step 1: create counter command
            await _bot.HandleMessage(CreateMessage(BotCommands.CREATE_COUNTER_COMMAND));
            Assert.AreEqual(BotCommands.CREATE_COUNTER_COMMAND, context.Command);
            Assert.IsNotNull(context.CreateCounterFlow);
            Assert.AreEqual(CreateFlowSteps.SetCounterName, context.CreateCounterFlow.State);

            //step 2: set counter name
            var counterName = "test-counter";
            await _bot.HandleMessage(CreateMessage(counterName));
            Assert.AreEqual(BotCommands.CREATE_COUNTER_COMMAND, context.Command);
            Assert.AreEqual(CreateFlowSteps.SetCounterStep, context.CreateCounterFlow.State);

            //step 3: set counter step
            var counterStep = 1;
            await _bot.HandleMessage(CreateMessage(counterStep.ToString()));
            Assert.AreEqual(BotCommands.SELECT_COUNTER_COMMAND, context.Command);
        }

        [Test]
        public async Task HandleMessage_DisplayAllCounters_SuccessTest()
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1,
            };

            _provider.Setup(x => x.GetContextAsync(It.IsAny<Message>())).ReturnsAsync(context);

            //ACT
            await _bot.HandleMessage(CreateMessage(BotCommands.DISPLAY_ALL_COUNTERS_COMMAND));

            //ASSERT
            Assert.AreEqual(BotCommands.SELECT_COUNTER_COMMAND, context.Command);
        }

        [Test]
        public async Task HandleMessage_BackCommandFromSelectMenu_SuccessTest()
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1,
            };
            context.SetCurrentCommand(BotCommands.SELECT_COUNTER_COMMAND);

            _provider.Setup(x => x.GetContextAsync(It.IsAny<Message>())).ReturnsAsync(context);

            //ACT
            await _bot.HandleMessage(CreateMessage(BotCommands.BACK_COMMAND));

            //ASSERT
            Assert.AreEqual(BotCommands.START_COMMAND, context.Command);
        }

        [Test]
        public async Task HandleMessage_BackCommandFromCounterMenu_SuccessTest()
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1,
            };
            context.SetCurrentCommand(BotCommands.MANAGE_COUNTER_COMMAND);

            _provider.Setup(x => x.GetContextAsync(It.IsAny<Message>())).ReturnsAsync(context);

            //ACT
            await _bot.HandleMessage(CreateMessage(BotCommands.BACK_COMMAND));

            //ASSERT
            Assert.AreEqual(BotCommands.SELECT_COUNTER_COMMAND, context.Command);
        }

        [Test]
        public async Task HandleMessage_BackCommandDefaultCase_SuccessTest()
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1,
            };
            context.SetCurrentCommand("lores ipsum");

            _provider.Setup(x => x.GetContextAsync(It.IsAny<Message>())).ReturnsAsync(context);

            //ACT
            await _bot.HandleMessage(CreateMessage(BotCommands.BACK_COMMAND));

            //ASSERT
            Assert.AreEqual(BotCommands.START_COMMAND, context.Command);
        }

        [Test]
        public async Task HandleMessage_SelectCounterCommand_SuccessTest()
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1,
            };
            context.SetCurrentCommand(BotCommands.SELECT_COUNTER_COMMAND);

            _provider.Setup(x => x.GetContextAsync(It.IsAny<Message>())).ReturnsAsync(context);

            //ACT
            await _bot.HandleMessage(CreateMessage("counter#1 - 0"));

            //ASSERT
            Assert.AreEqual(BotCommands.MANAGE_COUNTER_COMMAND, context.Command);
        }

        [TestCase(BotCommands.INCREMENT_COMMAND)]
        [TestCase(BotCommands.DECREMENT_COMMAND)]
        public async Task HandleMessage_IncrementDecrement_SuccessTest(string command)
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1,
            };

            var amount = 1;
            ushort step = 1;

            context.SelectCounter(new Domain.Models.Counter("test", amount, step, true));

            _provider.Setup(x => x.GetContextAsync(It.IsAny<Message>())).ReturnsAsync(context);

            //ACT
            await _bot.HandleMessage(CreateMessage(command));

            //ASSERT
            var expected = command == BotCommands.INCREMENT_COMMAND ? (amount + step) : (amount - step);
            Assert.AreEqual(expected, context.SelectedCounter.Amount);
        }

        private static Message CreateMessage(string text, int id = 1)
        {
            return new Message
            {
                Text = text,
                Chat = new Chat
                {
                    Id = id
                },
                From = new User 
                {
                    Id = id,
                    FirstName = "test",
                    LastName = "test",
                    Username = "@test"
                }
            };
        }
    }
}
