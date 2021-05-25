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
using System.Text;
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
        [Test]
        public void Job_DoesntTrhow()
        {
            //ARRANGE
            var counter1 = GetCounter();
            var user1 = GetUser();

            var counters = new List<CounterDto>
            {
                counter1
            };

            var users = new List<Domain.Models.User> 
            {
                user1
            };

            var counterStore = new Mock<ICounterStore>();
            var logger = new Mock<ILogger<ProcessCountersJob>>();
            var bot = new Mock<ITelegramBotClient>();
            var userStore = new Mock<IUserStore>();
            var metrics = new Mock<IMetricsRoot>();
            var measure = new Mock<IMeasureMetrics>();
            var counterMetric = new Mock<IMeasureCounterMetrics>();
            var context = new Mock<IJobExecutionContext>();

            counterStore.Setup(x => x.GetCountersAsync()).ReturnsAsync(counters);
            counterStore.Setup(x => x.UpdateManyAsync(It.IsAny<IEnumerable<Counter>>()))
                .Returns<IEnumerable<Counter>>(list => 
                {
                    counter1.Amount = list.ToArray()[0].Amount;
                    return Task.CompletedTask;
                });

            userStore.Setup(x => x.GetUsersById(It.IsAny<IEnumerable<int>>())).ReturnsAsync(users);

            measure.Setup(x => x.Counter).Returns(counterMetric.Object);
            metrics.Setup(x => x.Measure).Returns(measure.Object);

            long? sentToChatId = null;
            bot.Setup(x => x.SendTextMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<IReplyMarkup>(), It.IsAny<CancellationToken>()))
                .Returns<ChatId, string, ParseMode, bool, bool, int, IReplyMarkup, CancellationToken>((chatId, _, __, ___, ____, _____, ______, _______) => 
                {
                    sentToChatId = chatId.Identifier;
                    return Task.FromResult(new Message()); 
                });

            var job = new ProcessCountersJob(counterStore.Object, logger.Object, bot.Object, userStore.Object, metrics.Object);

            //ACT
            var oldAmount = counter1.Amount;
            AsyncTestDelegate act = async() => await job.Execute(context.Object);

            //ASSERT
            Assert.DoesNotThrowAsync(act);
            Assert.AreEqual(oldAmount + counter1.Step, counter1.Amount);
            Assert.IsTrue(sentToChatId.HasValue);
            Assert.AreEqual(user1.TelegramChatId, sentToChatId.Value);
        }

        private static CounterDto GetCounter()
        {
            return new CounterDto
            {
                Amount = 0,
                Step = 1,
                Id = Guid.NewGuid(),
                UserId = 1,
                Title = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                IsManual = false
            };
        }

        private static Domain.Models.User GetUser()
        {
            return new Domain.Models.User
            {
                TelegramId = 1,
                TelegramChatId = 1
            };
        }
    }
}
