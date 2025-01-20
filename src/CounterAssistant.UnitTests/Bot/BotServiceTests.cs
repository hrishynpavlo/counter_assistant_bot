using App.Metrics;
using App.Metrics.Counter;
using CounterAssistant.Bot;
using CounterAssistant.Bot.Flows;
using CounterAssistant.Bot.Formatters;
using CounterAssistant.DataAccess;
using CounterAssistant.Domain.Models;
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
        private Mock<IMeasureCounterMetrics> _counterMetric;

        [SetUp]
        public void Init()
        {
            _counterMetric = new Mock<IMeasureCounterMetrics>();
            var measure = new Mock<IMeasureMetrics>();
            measure.Setup(x => x.Counter).Returns(_counterMetric.Object);
            _metrics = new Mock<IMetricsRoot>();
            _metrics.Setup(x => x.Measure).Returns(measure.Object);

            var counterStore = new Mock<ICounterService>();
            counterStore.Setup(x => x.GetUserCountersAsync(It.IsAny<long>())).ReturnsAsync(new List<Domain.Models.Counter>());
            counterStore.Setup(x => x.GetCounterByBotRequstAsync(It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(new Domain.Models.Counter("test", 1, 1, true, CounterUnit.Time));

            var logger = new Mock<ILogger<BotService>>();
            var botClient = new Mock<ITelegramBotClient>();

            _provider = new Mock<IContextProvider>();
            _bot = new BotService(botClient.Object, _provider.Object, counterStore.Object, new BotMessageFormatter(), logger.Object, _metrics.Object);
        }

        [Test]
        public void HandleRequest_NotCommand_SuccessTest()
        {
            //ARRANGE
            _provider.Setup(x => x.GetContextAsync(It.IsAny<BotRequest>())).ReturnsAsync(new ChatContext { });

            var request = CreateRequest(text: "lores ipsum");

            //ACT
            var act = new AsyncTestDelegate(async() => await _bot.HandleRequest(request));

            //ASSERT
            Assert.DoesNotThrowAsync(act);
        }

        [Test]
        public async Task HandleRequest_StartCommand_SuccessTest()
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1,
            };

            _provider.Setup(x => x.GetContextAsync(It.IsAny<BotRequest>())).ReturnsAsync(context);

            //ACT
            await _bot.HandleRequest(CreateRequest(BotCommands.START_COMMAND));

            //ASSERT
            Assert.That(BotCommands.START_COMMAND, Is.EqualTo(context.Command));
        }

        [Test]
        public async Task HandleRequest_CreateCounterFullFlow_SuccessTest()
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1,
            };

            _provider.Setup(x => x.GetContextAsync(It.IsAny<BotRequest>())).ReturnsAsync(context);

            //ACT && ASSERT

            //step 1: create counter command
            await _bot.HandleRequest(CreateRequest(BotCommands.CREATE_COUNTER_COMMAND));
            Assert.That(BotCommands.CREATE_COUNTER_COMMAND, Is.EqualTo(context.Command));
            Assert.That(context.CreateCounterFlow, Is.Not.Null);
            Assert.That(CreateFlowSteps.SetCounterName, Is.EqualTo(context.CreateCounterFlow.State));

            //step 2: set counter name
            var counterName = "test-counter";
            await _bot.HandleRequest(CreateRequest(counterName));
            Assert.That(BotCommands.CREATE_COUNTER_COMMAND, Is.EqualTo(context.Command));
            Assert.That(CreateFlowSteps.SetCounterStep, Is.EqualTo(context.CreateCounterFlow.State));

            //step 3: set counter step
            var counterStep = 1;
            await _bot.HandleRequest(CreateRequest(counterStep.ToString()));
            Assert.That(BotCommands.CREATE_COUNTER_COMMAND, Is.EqualTo(context.Command));
            Assert.That(CreateFlowSteps.SetCounterType, Is.EqualTo(context.CreateCounterFlow.State));

            //step 4: set counter type
            var type = CounterType.Automatic;
            await _bot.HandleRequest(CreateRequest(type.ToString()));
            Assert.That(BotCommands.CREATE_COUNTER_COMMAND, Is.EqualTo(context.Command));
            Assert.That(CreateFlowSteps.SetCounterUnit, Is.EqualTo(context.CreateCounterFlow.State));

            //step 5: set counter unit
            var unit = CounterUnit.Day;
            await _bot.HandleRequest(CreateRequest(unit.ToString()));
            Assert.That(BotCommands.SELECT_COUNTER_COMMAND, Is.EqualTo(context.Command));
            Assert.That(context.CreateCounterFlow, Is.Null);
        }

        [Test]
        public async Task HandleRequest_DisplayAllCounters_SuccessTest()
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1,
            };

            _provider.Setup(x => x.GetContextAsync(It.IsAny<BotRequest>())).ReturnsAsync(context);

            //ACT
            await _bot.HandleRequest(CreateRequest(BotCommands.DISPLAY_ALL_COUNTERS_COMMAND));

            //ASSERT
            Assert.That(BotCommands.SELECT_COUNTER_COMMAND, Is.EqualTo(context.Command));
        }

        [Test]
        public async Task HandleRequest_BackCommandFromSelectMenu_SuccessTest()
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1,
            };
            context.SetCurrentCommand(BotCommands.SELECT_COUNTER_COMMAND);

            _provider.Setup(x => x.GetContextAsync(It.IsAny<BotRequest>())).ReturnsAsync(context);

            //ACT
            await _bot.HandleRequest(CreateRequest(BotCommands.BACK_COMMAND));

            //ASSERT
            Assert.That(BotCommands.START_COMMAND, Is.EqualTo(context.Command));
        }

        [Test]
        public async Task HandleRequest_BackCommandFromCounterMenu_SuccessTest()
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1,
            };
            context.SetCurrentCommand(BotCommands.MANAGE_COUNTER_COMMAND);

            _provider.Setup(x => x.GetContextAsync(It.IsAny<BotRequest>())).ReturnsAsync(context);

            //ACT
            await _bot.HandleRequest(CreateRequest(BotCommands.BACK_COMMAND));

            //ASSERT
            Assert.That(BotCommands.SELECT_COUNTER_COMMAND, Is.EqualTo(context.Command));
        }

        [Test]
        public async Task HandleRequest_BackCommandDefaultCase_SuccessTest()
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1,
            };
            context.SetCurrentCommand("lores ipsum");

            _provider.Setup(x => x.GetContextAsync(It.IsAny<BotRequest>())).ReturnsAsync(context);

            //ACT
            await _bot.HandleRequest(CreateRequest(BotCommands.BACK_COMMAND));

            //ASSERT
            Assert.That(BotCommands.START_COMMAND, Is.EqualTo(context.Command));
        }

        [Test]
        public async Task HandleRequest_SelectCounterCommand_SuccessTest()
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1,
            };
            context.SetCurrentCommand(BotCommands.SELECT_COUNTER_COMMAND);

            _provider.Setup(x => x.GetContextAsync(It.IsAny<BotRequest>())).ReturnsAsync(context);

            //ACT
            await _bot.HandleRequest(CreateRequest("counter#1 - 0"));

            //ASSERT
            Assert.That(BotCommands.MANAGE_COUNTER_COMMAND, Is.EqualTo(context.Command));
        }

        [TestCase(BotCommands.INCREMENT_COMMAND)]
        [TestCase(BotCommands.DECREMENT_COMMAND)]
        public async Task HandleRequest_IncrementDecrementCommand_SuccessTest(string command)
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1,
            };

            var amount = 1;
            ushort step = 1;

            context.SelectCounter(new Domain.Models.Counter("test", amount, step, true, CounterUnit.Time));

            _provider.Setup(x => x.GetContextAsync(It.IsAny<BotRequest>())).ReturnsAsync(context);

            //ACT
            await _bot.HandleRequest(CreateRequest(command));

            //ASSERT
            var expected = command == BotCommands.INCREMENT_COMMAND ? (amount + step) : (amount - step);
            Assert.That(expected, Is.EqualTo(context.SelectedCounter.Amount));
        }

        [Test]
        public async Task HandleRequest_ResetCounter_SuccessTest()
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1,
            };

            context.SelectCounter(new Domain.Models.Counter("test", 100, 1, true, CounterUnit.Time));
            var lastModifiedBeforeUpdate = context.SelectedCounter.LastModifiedAt;

            _provider.Setup(x => x.GetContextAsync(It.IsAny<BotRequest>())).ReturnsAsync(context);

            //ACT
            await _bot.HandleRequest(CreateRequest(BotCommands.RESET_COUNTER_COMMAND));

            //ASSERT
            Assert.That(0, Is.EqualTo(context.SelectedCounter.Amount));
            Assert.That(context.SelectedCounter.LastModifiedAt, Is.GreaterThan(lastModifiedBeforeUpdate));
        }

        [Test]
        public async Task HandleRequest_RemoveCounterCommand_SuccessTest()
        {
            //ARRANGE
            var context = new ChatContext
            {
                ChatId = 1
            };

            context.SelectCounter(new Counter("test", 0, 1, true, CounterUnit.Time));

            _provider.Setup(x => x.GetContextAsync(It.IsAny<BotRequest>())).ReturnsAsync(context);

            //ACT
            await _bot.HandleRequest(CreateRequest(BotCommands.REMOVE_COUNTER_COMMAND));

            //ASSERT
            Assert.That(context.SelectedCounter, Is.Null);
            Assert.That(BotCommands.START_COMMAND, Is.EqualTo(context.Command));
        }

        [Test]
        public void HandleRequest_CreateCounterFlowCompleted_DoesntThrowButCaptureTheError()
        {
            //ARRANGE
            var user = new Domain.Models.User 
            {
                TelegramId = 1,
                BotInfo = new Domain.Models.UserBotInfo
                {
                    ChatId = 1,
                    LastCommand = BotCommands.CREATE_COUNTER_COMMAND,
                    CreateCounterFlowInfo = new Domain.Models.CreateCounterFlowInfo 
                    {
                        State = CreateFlowSteps.Completed.ToString()
                    }
                }
            };

            var context = ChatContext.Restore(user, null);

            var wasErrors = false;
            _counterMetric.Setup(x => x.Increment(It.IsAny<CounterOptions>())).Callback(() =>
            {
                wasErrors = true;
            });

            _provider.Setup(x => x.GetContextAsync(It.IsAny<BotRequest>())).ReturnsAsync(context);

            //ACT
            var act = new AsyncTestDelegate(async() => await _bot.HandleRequest(CreateRequest("test")));

            //ASSERT
            Assert.DoesNotThrowAsync(act);
            Assert.That(wasErrors, Is.True);
        }

        private static BotRequest CreateRequest(string text, int id = 1)
        {
            var message = new Message
            {
                Text = text,
                Chat = new Chat
                {
                    Id = id
                },
                From = new Telegram.Bot.Types.User
                {
                    Id = id,
                    FirstName = "test",
                    LastName = "test",
                    Username = "@test"
                }
            };

            return BotRequest.FromMessage(message); 
        }
    }
}
