using App.Metrics;
using App.Metrics.Counter;
using CounterAssistant.API.Jobs;
using CounterAssistant.Bot.Formatters;
using CounterAssistant.DataAccess;
using CounterAssistant.DataAccess.DTO;
using CounterAssistant.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CounterAssistant.UnitTests.Jobs
{
    [TestFixture]
    public class ProcessCountersJobTests
    {
        private Mock<ILogger<ProcessCountersJob>> Logger;
        private Mock<IMetricsRoot> Metrics;
        private Mock<IJobExecutionContext> JobContext; 

        [OneTimeSetUp]
        public void Init()
        {
            Logger = new Mock<ILogger<ProcessCountersJob>>();
            Metrics = new Mock<IMetricsRoot>();

            var measure = new Mock<IMeasureMetrics>();
            var counterMetric = new Mock<IMeasureCounterMetrics>();
            measure.Setup(x => x.Counter).Returns(counterMetric.Object);
            Metrics.Setup(x => x.Measure).Returns(measure.Object);

            JobContext = new Mock<IJobExecutionContext>();
        }

        [Test]
        public void ProcessCountersJob_ACoupleOfUsersWithACoupleOfCounters_DoesntTrhow()
        {
            //ARRANGE
            var user1 = GetUser(1);
            var user2 = GetUser(2);

            var counter1_1 = GetCounter(user1.TelegramId);
            var counter1_2 = GetCounter(user1.TelegramId);

            var counter2_1 = GetCounter(user2.TelegramId, 2);
            var counter2_2 = GetCounter(user2.TelegramId, 3, 10);

            var startTime = DateTime.UtcNow;

            var counters = new Dictionary<int, Counter[]>
            {
                [user1.TelegramId] = new[] { counter1_1, counter1_2 },
                [user2.TelegramId] = new[] { counter2_1, counter2_2 }
            };

            var users = new Dictionary<int, Domain.Models.User>
            {
                [user1.TelegramId] = user1,
                [user2.TelegramId] = user2
            };

            var counterService = new Mock<ICounterService>();
            var bot = new Mock<ITelegramBotClient>();
            var userService = new Mock<IUserService>();

            var numberOfUpdatedCountes = 0;
            counterService.Setup(x => x.GetCountersForDailyUpdateAsync()).ReturnsAsync(counters);
            counterService.Setup(x => x.BulkUpdateAmountAsync(It.IsAny<IEnumerable<Counter>>()))
                .Returns<IEnumerable<Counter>>(list => 
                {
                    foreach(var updatedCounter in list)
                    {
                        numberOfUpdatedCountes++;
                    }

                    return Task.CompletedTask;
                });

            userService.Setup(x => x.GetUsersByIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(users);

            var chatIds = new HashSet<long>();
            bot.Setup(x => x.SendTextMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<IReplyMarkup>(), It.IsAny<CancellationToken>()))
                .Returns<ChatId, string, ParseMode, bool, bool, int, IReplyMarkup, CancellationToken>((chatId, _, __, ___, ____, _____, ______, _______) => 
                {
                    chatIds.Add(chatId.Identifier);
                    return Task.FromResult(new Message()); 
                });

            var job = new ProcessCountersJob(counterService.Object, bot.Object, userService.Object, new BotMessageFormatter(), Logger.Object, Metrics.Object);

            //ACT
            AsyncTestDelegate act = async() => await job.Execute(JobContext.Object);

            //ASSERT
            Assert.DoesNotThrowAsync(act);
            Assert.AreEqual(2, chatIds.Count);
            Assert.Multiple(() => 
            {
                Assert.IsTrue(chatIds.Contains(user1.BotInfo.ChatId));
                Assert.IsTrue(chatIds.Contains(user2.BotInfo.ChatId));
            });

            Assert.AreEqual(counters.Values.SelectMany(x => x).Count(), numberOfUpdatedCountes);
            Assert.IsTrue(counters.Values.SelectMany(x => x).All(x => x.LastModifiedAt > startTime));
        }

        [Test]
        public void ProcessCountersJob_UsersDoesNotExist_DoesntTrhow()
        {
            //ARRANGE
            var user = GetUser(1);
            var counter = GetCounter(user.TelegramId);

            var counterService = new Mock<ICounterService>();
            counterService.Setup(x => x.GetCountersForDailyUpdateAsync()).ReturnsAsync(new Dictionary<int, Counter[]> { [user.TelegramId] = new[] { counter } });

            var wasMessagesSent = false;
            var bot = new Mock<ITelegramBotClient>();
            bot.Setup(x => x.SendTextMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<IReplyMarkup>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(() => 
               {
                   wasMessagesSent = true;
                   return new Message(); 
               });

            var userService = new Mock<IUserService>();
            userService.Setup(x => x.GetUsersByIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(new Dictionary<int, Domain.Models.User>());

            var job = new ProcessCountersJob(counterService.Object, bot.Object, userService.Object, new BotMessageFormatter(), Logger.Object, Metrics.Object);

            //ACT
            AsyncTestDelegate act = async () => await job.Execute(JobContext.Object);

            //ASSERT
            Assert.DoesNotThrowAsync(act);
            Assert.IsFalse(wasMessagesSent);
        }

        private static Counter GetCounter(int userId, ushort step = 1, int amount = 0)
        {
            return new Counter(id: Guid.NewGuid(), Guid.NewGuid().ToString(), amount, step, DateTime.UtcNow, null, false, CounterUnit.Time);
        }

        private static Domain.Models.User GetUser(int id)
        {
            return new Domain.Models.User
            {
                TelegramId = id,
                BotInfo = new UserBotInfo
                {
                    ChatId = id
                }
            };
        }
    }
}
