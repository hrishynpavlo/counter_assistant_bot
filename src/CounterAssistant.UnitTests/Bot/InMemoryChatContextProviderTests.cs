using App.Metrics;
using App.Metrics.Counter;
using CounterAssistant.Bot;
using CounterAssistant.Bot.Flows;
using CounterAssistant.DataAccess;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace CounterAssistant.UnitTests.Bot
{
    [TestFixture]
    public class InMemoryChatContextProviderTests
    {
        private Mock<IUserService> _userService;
        private Mock<ICounterService> _counterStore;
        private Mock<IMemoryCache> _cache;
        private Mock<IMetricsRoot> _metrics;
        private Mock<ILogger<InMemoryContextProvider>> _logger;

        private readonly static ContextProviderSettings _settings = new ContextProviderSettings
        {
            ExpirationTime = TimeSpan.FromMinutes(30),
            ProlongationTime = TimeSpan.FromMinutes(3)
        };

        [SetUp]
        public void Init()
        {
            _userService = new Mock<IUserService>();
            _counterStore = new Mock<ICounterService>();
            _cache = new Mock<IMemoryCache>();
            _metrics = new Mock<IMetricsRoot>();

            var counter = new Mock<IMeasureCounterMetrics>();
            var measure = new Mock<IMeasureMetrics>();
            measure.Setup(x => x.Counter).Returns(counter.Object);
            _metrics.Setup(x => x.Measure).Returns(measure.Object);

            _logger = new Mock<ILogger<InMemoryContextProvider>>();
        }

        [Test]
        public void GetContext_NullMessage_ThrowsArgumentNullException()
        {
            //ARRANGE
            var contextProvider = new InMemoryContextProvider(_userService.Object, _counterStore.Object, _cache.Object, _settings, _metrics.Object, _logger.Object);

            //ACT
            var act = new AsyncTestDelegate(async () => await contextProvider.GetContextAsync(null));

            //ASSERT
            Assert.ThrowsAsync<ArgumentNullException>(act);
        }

        [Test]
        public async Task GetContext_ExistingUserFromCache_SuccessTest()
        {
            //ARRANGE
            var request = new BotRequest(new User { Id = 1 }, 1, "test");

            var context = new ChatContext 
            {
                UserId = request.UserId,
                UserName = "@test",
                ChatId = 1001
            };

            var cache = new MemoryCache(new MemoryCacheOptions());
            cache.Set(request.UserId, context);

            var contextProvider = new InMemoryContextProvider(_userService.Object, _counterStore.Object, cache, _settings, _metrics.Object, _logger.Object);

            //ACT
            var result = await contextProvider.GetContextAsync(request);

            //ASSERT
            Assert.That(result, Is.Not.Null);
            Assert.That(context.UserId, Is.EqualTo(result.UserId));
            Assert.That(context.UserName, Is.EqualTo(result.UserName));
            Assert.That(context.ChatId, Is.EqualTo(result.ChatId));
        }

        [Test]
        public async Task GetContext_ExistingUserFromDb_SuccessTest()
        {
            //ARRANGE
            var request = new BotRequest(new User { Id = 1 }, 1, "test");

            var user = new Domain.Models.User 
            {
                TelegramId = request.UserId,
                BotInfo = new Domain.Models.UserBotInfo 
                {
                    ChatId = 1001,
                    CreateCounterFlowInfo = new Domain.Models.CreateCounterFlowInfo 
                    {
                        State = CreateFlowSteps.None.ToString(),
                        Args = new Dictionary<string, object>()
                    },
                    LastCommand = BotCommands.SETTINGS_COMMAND,
                    SelectedCounterId = null
                }
            };

            var cache = new MemoryCache(new MemoryCacheOptions());
            _userService.Setup(x => x.GetUserByIdAsync(It.IsAny<long>())).ReturnsAsync(user);

            var contextProvider = new InMemoryContextProvider(_userService.Object, _counterStore.Object, cache, _settings, _metrics.Object, _logger.Object);

            //ACT
            var context = await contextProvider.GetContextAsync(request);

            //ASSERT
            Assert.That(context, Is.Not.Null);
            Assert.That(cache.TryGetValue(context.UserId, out var _), Is.True);
            Assert.That(user.BotInfo.ChatId, Is.EqualTo(context.ChatId));
            Assert.That(user.BotInfo.LastCommand, Is.EqualTo(context.Command));
            Assert.That(context.CreateCounterFlow, Is.Not.Null);
            Assert.That(user.BotInfo.CreateCounterFlowInfo.State, Is.EqualTo(context.CreateCounterFlow.State.ToString()));
            Assert.That(context.SelectedCounter, Is.Null);
        }

        [Test]
        public async Task GetContext_NotExistingUser_SuccessTest()
        {
            //ARRANGE
            var message = new Message
            {
                From = new User
                {
                    Id = 1,
                    FirstName = "test",
                    LastName = "test",
                    Username = "@test"
                },
                Chat = new Chat
                {
                    Id = 1011
                }
            };

            var request = BotRequest.FromMessage(message);

            var cache = new MemoryCache(new MemoryCacheOptions());

            var contextProvider = new InMemoryContextProvider(_userService.Object, _counterStore.Object, cache, _settings, _metrics.Object, _logger.Object);

            //ACT
            var context = await contextProvider.GetContextAsync(request);

            //ASSERT
            Assert.That(context, Is.Not.Null);
            Assert.That(message.From.Id, Is.EqualTo(context.UserId));
            Assert.That(message.From.Username, Is.EqualTo(context.UserName));
            Assert.That($"{message.From.FirstName} {message.From.LastName}", Is.EqualTo(context.Name));
            Assert.That(message.Chat.Id, Is.EqualTo(context.ChatId));
            Assert.That(BotCommands.START_COMMAND, Is.EqualTo(context.Command));
            Assert.That(context.CreateCounterFlow, Is.Not.Null);
            Assert.That(CreateFlowSteps.None, Is.EqualTo(context.CreateCounterFlow.State));
            Assert.That(context.SelectedCounter, Is.Null);
        }
    }
}
