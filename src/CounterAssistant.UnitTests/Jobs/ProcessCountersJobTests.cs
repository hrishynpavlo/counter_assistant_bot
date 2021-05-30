using App.Metrics;
using App.Metrics.Counter;
using CounterAssistant.API.Jobs;
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

            var counters = new List<CounterDto>
            {
                counter1_1, counter1_2, counter2_1, counter2_2
            };

            var users = new List<Domain.Models.User> 
            {
                user1, user2
            };

            var counterStore = new Mock<ICounterStore>();
            var bot = new Mock<ITelegramBotClient>();
            var userStore = new Mock<IUserStore>();

            counterStore.Setup(x => x.GetCountersAsync()).ReturnsAsync(counters);
            counterStore.Setup(x => x.UpdateManyAsync(It.IsAny<IEnumerable<Counter>>()))
                .Returns<IEnumerable<Counter>>(list => 
                {
                    foreach(var updatedCounter in list)
                    {
                        counters.FirstOrDefault(x => x.Id == updatedCounter.Id).Amount = updatedCounter.Amount;
                    }

                    return Task.CompletedTask;
                });

            userStore.Setup(x => x.GetUsersById(It.IsAny<IEnumerable<int>>())).ReturnsAsync(users);

            var chatIds = new HashSet<long>();
            bot.Setup(x => x.SendTextMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<IReplyMarkup>(), It.IsAny<CancellationToken>()))
                .Returns<ChatId, string, ParseMode, bool, bool, int, IReplyMarkup, CancellationToken>((chatId, _, __, ___, ____, _____, ______, _______) => 
                {
                    chatIds.Add(chatId.Identifier);
                    return Task.FromResult(new Message()); 
                });

            var job = new ProcessCountersJob(counterStore.Object, bot.Object, userStore.Object, Logger.Object, Metrics.Object);

            //ACT
            var oldAmounts = counters.ToDictionary(x => x.Id, x => x.Amount);
            AsyncTestDelegate act = async() => await job.Execute(JobContext.Object);

            //ASSERT
            Assert.DoesNotThrowAsync(act);
            Assert.AreEqual(2, chatIds.Count);
            Assert.Multiple(() => 
            {
                Assert.IsTrue(chatIds.Contains(user1.BotInfo.ChatId));
                Assert.IsTrue(chatIds.Contains(user2.BotInfo.ChatId));
            });

            Assert.Multiple(() =>
            {
                foreach (var counter in counters)
                {
                    Assert.IsTrue(oldAmounts.TryGetValue(counter.Id, out var oldAmount));
                    Assert.AreEqual(counter.Amount, oldAmount + counter.Step);
                }
            });
        }

        [Test]
        public void ProcessCountersJob_UsersDoesNotExist_DoesntTrhow()
        {
            //ARRANGE
            var user = GetUser(1);
            var counter = GetCounter(user.TelegramId);

            var counterStore = new Mock<ICounterStore>();
            counterStore.Setup(x => x.GetCountersAsync()).ReturnsAsync(new List<CounterDto> { counter });

            var wasMessagesSent = false;
            var bot = new Mock<ITelegramBotClient>();
            bot.Setup(x => x.SendTextMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<IReplyMarkup>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(() => 
               {
                   wasMessagesSent = true;
                   return new Message(); 
               });

            var userStore = new Mock<IUserStore>();
            userStore.Setup(x => x.GetUsersById(It.IsAny<IEnumerable<int>>())).ReturnsAsync(new List<Domain.Models.User>());

            var job = new ProcessCountersJob(counterStore.Object, bot.Object, userStore.Object, Logger.Object, Metrics.Object);

            //ACT
            AsyncTestDelegate act = async () => await job.Execute(JobContext.Object);

            //ASSERT
            Assert.DoesNotThrowAsync(act);
            Assert.IsFalse(wasMessagesSent);
        }

        private static CounterDto GetCounter(int userId, ushort step = 1, int amount = 0)
        {
            return new CounterDto
            {
                Amount = amount,
                Step = step,
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                IsManual = false
            };
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
