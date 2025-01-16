using App.Metrics;
using CounterAssistant.Bot.Extensions;
using CounterAssistant.Bot.Formatters;
using CounterAssistant.DataAccess;
using CounterAssistant.Domain.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static CounterAssistant.Bot.BotCommands;

namespace CounterAssistant.Bot
{
    public class BotService : IHostedService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IContextProvider _contextProvider;
        private readonly ICounterService _counterService;
        private readonly IBotMessageFormatter _messageFormatter;
        private readonly ILogger<BotService> _logger;
        private readonly IMetricsRoot _metrics;

        private static readonly ReceiverOptions _receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = {}
        };
        
        private static readonly ReplyKeyboardMarkup DEFAULT_KEYBOARD = new ReplyKeyboardMarkup(keyboard: 
            new List<List<KeyboardButton>>
            {
                new List<KeyboardButton>
                {
                    new KeyboardButton(CREATE_COUNTER_COMMAND),
                    new KeyboardButton(SETTINGS_COMMAND)
                },
                new List<KeyboardButton>
                {
                    new KeyboardButton(DISPLAY_ALL_COUNTERS_COMMAND)
                }
            })
        {
            ResizeKeyboard = true
        };
        private static readonly ReplyKeyboardMarkup COUNTER_KEYBOARD = new ReplyKeyboardMarkup(keyboard: 
            new List<List<KeyboardButton>>
            {
                new List<KeyboardButton>
                {
                    new KeyboardButton(DECREMENT_COMMAND),
                    new KeyboardButton(INCREMENT_COMMAND),
                },
                new List<KeyboardButton>
                {
                    new KeyboardButton(RESET_COUNTER_COMMAND),
                    new KeyboardButton(REMOVE_COUNTER_COMMAND)
                },
                new List<KeyboardButton>
                {
                    new KeyboardButton(BACK_COMMAND)
                }
            })
        {
            ResizeKeyboard = true
        };

        public BotService(ITelegramBotClient botClient, IContextProvider contextProvider, ICounterService counterService, IBotMessageFormatter messageFormatter, ILogger<BotService> logger, IMetricsRoot metrics)
        {
            _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
            _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
            _counterService = counterService ?? throw new ArgumentNullException(nameof(counterService));
            _messageFormatter = messageFormatter ?? throw new ArgumentNullException(nameof(messageFormatter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        }

        [ExcludeFromCodeCoverage]
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var commands = new[]
            {
                new BotCommand { Command = START_COMMAND, Description = "старт" }
            };

            await _botClient.SetMyCommands(commands, cancellationToken: cancellationToken);

            _botClient.StartReceiving(
                HandleMessage, 
                HandleError, 
                _receiverOptions, 
                cancellationToken);
        }

        private async Task HandleMessage(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    await OnMessageHandler(update.Message);
                    break;
                case UpdateType.CallbackQuery:
                    await OnCallbackQueryHandler(update.CallbackQuery);
                    break;
                default:
                    _logger.LogWarning("Not handled update type = {type} received", update.Type);
                    break;
            }
        }

        private Task HandleError(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Update polling error, see details:");
            return Task.CompletedTask;
        }
        
        [ExcludeFromCodeCoverage(Justification = "There is no way to create MessageEventArgs")]
        private async Task OnMessageHandler(Message message)
        {
            if (message.Text != null)
            {
                var request = BotRequest.FromMessage(message);
                await HandleRequest(request);
            }
        }

        [ExcludeFromCodeCoverage(Justification = "There is no way to create CallbackQueryEventArgs")]
        private async Task OnCallbackQueryHandler(CallbackQuery callbackQuery)
        {
            if (callbackQuery.Data != null)
            {
                var request = BotRequest.FromCallback(callbackQuery);
                await HandleRequest(request);
            }
        }

        public async Task HandleRequest(BotRequest request)
        {
            _metrics.Measure.Counter.Increment(BotMetrics.RecievedMessages);

            var context = await _contextProvider.GetContextAsync(request);
            var message = request.Text;

            try
            {
                switch (message)
                {
                    case START_COMMAND:
                    {
                        context.SetCurrentCommand(START_COMMAND);
                        _logger.LogInformation("User {user} has started bot", context.UserId);
                        _metrics.Measure.Counter.Increment(BotMetrics.StartedChats);
                        await _botClient.SendMessage(context.ChatId, $"Привет {context.Name}, меня зовут Джарвис, я счётчик-бот и хочу облегчить тебе жизнь!", replyMarkup: DEFAULT_KEYBOARD);
                        break;
                    }
                    case CREATE_COUNTER_COMMAND:
                    case not null when context.Command == CREATE_COUNTER_COMMAND:
                    {
                        context.SetCurrentCommand(CREATE_COUNTER_COMMAND);

                        if (context.CreateCounterFlow == null) context.StartCreateCounterFlow();
                        var result = context.CreateCounterFlow.Perform(message);

                        if (!result.IsCompleted)
                        {
                            await _botClient.SendMessage(context.ChatId, result.Message, replyMarkup: result.Buttons);
                        }
                        else
                        {
                            context.SetCurrentCommand(SELECT_COUNTER_COMMAND);
                            context.FinishCreateCounterFlow();

                            await _counterService.CreateAsync(result.Counter, context.UserId);
                            _logger.LogInformation("user {user} has successfully created counter {id}", context.UserId, result.Counter.Id);

                            var counters = await _counterService.GetUserCountersAsync(context.UserId);

                            await _botClient.SendMessage(context.ChatId, result.Message, parseMode: ParseMode.Html, replyMarkup: GetCounterKeyboard(counters));
                        }
                        break;
                    }
                    case SETTINGS_COMMAND:
                    {
                        // todo
                        await _botClient.SendMessage(context.ChatId, text: "Эта фича еще в разработке", replyMarkup: DEFAULT_KEYBOARD);
                        break;
                    }
                    case DISPLAY_ALL_COUNTERS_COMMAND:
                    {
                        var counters = await _counterService.GetUserCountersAsync(context.UserId);
                        context.SetCurrentCommand(SELECT_COUNTER_COMMAND);
                        await _botClient.SendMessage(context.ChatId, text: _messageFormatter.GetDetailedCounters(counters), parseMode: ParseMode.Html, replyMarkup: GetCounterKeyboard(counters));
                        break;
                    }
                    case BACK_COMMAND when context.Command == MANAGE_COUNTER_COMMAND:
                    {
                        context.SetCurrentCommand(SELECT_COUNTER_COMMAND);

                        await _counterService.UpdateAmountAsync(context.SelectedCounter);
                        context.ClearSelectedCounter();

                        var counters = await _counterService.GetUserCountersAsync(context.UserId);
                        await _botClient.SendMessage(context.ChatId, text: "Выберите счётчик: " , parseMode: ParseMode.Html, replyMarkup: GetCounterKeyboard(counters));

                        break;
                    }
                    case BACK_COMMAND:
                    {
                        // for SELECT_COUNTER_COMMAND and default command
                        context.SetCurrentCommand(START_COMMAND);
                        await _botClient.SendMessage(context.ChatId, "Выберите действие:", replyMarkup: DEFAULT_KEYBOARD);
                        break;
                    }
                    case not null when context.Command == SELECT_COUNTER_COMMAND:
                    {
                        var counterName = message.Substring(0, message.IndexOf(" -"));
                        var counter = await _counterService.GetCounterByBotRequstAsync(context.UserId, counterName);
                        context.SelectCounter(counter);

                        context.SetCurrentCommand(MANAGE_COUNTER_COMMAND);

                        await _botClient.SendMessage(context.ChatId, text: _messageFormatter.GetDetailedCounter(context.SelectedCounter), parseMode: ParseMode.Html, replyMarkup: COUNTER_KEYBOARD);
                        break;
                    }
                    case DECREMENT_COMMAND:
                    {
                        context.SelectedCounter.Decrement();
                        await _botClient.SendMessage(context.ChatId, $"Счётчик успешно уменьшен: <b>{context.SelectedCounter.Title}: {context.SelectedCounter.Amount}</b>", parseMode: ParseMode.Html);
                        break;
                    }
                    case INCREMENT_COMMAND:
                    {
                        context.SelectedCounter.Increment();
                        await _botClient.SendMessage(context.ChatId, $"Счётчик успешно увеличен: <b>{context.SelectedCounter.Title}: {context.SelectedCounter.Amount}</b>.", parseMode: ParseMode.Html);
                        break;
                    }
                    case RESET_COUNTER_COMMAND:
                    {
                        context.SelectedCounter.Reset();
                        await _botClient.SendMessage(context.ChatId, $"Значение счётчика <b>{context.SelectedCounter.Title}</b> успешно сброшено до 0.", parseMode: ParseMode.Html);
                        break;
                    }
                    case REMOVE_COUNTER_COMMAND:
                    {
                        await _counterService.RemoveAsync(context.SelectedCounter.Id);
                        var counterName = context.SelectedCounter.Title;
                        context.ClearSelectedCounter();
                        context.SetCurrentCommand(START_COMMAND);
                        _metrics.Measure.Counter.Increment(BotMetrics.RemovedCounters);
                        await _botClient.SendMessage(context.ChatId, $"Счётчик <b>{counterName}</b> успешно удален. Выберите другой счётчик:", parseMode: ParseMode.Html, replyMarkup: DEFAULT_KEYBOARD);
                        break;
                    }
                    default:
                    {
                        _logger.LogInformation("user {id} message: {msg} is not recognized as a bot command", context.UserId, message);
                        await _botClient.SendMessage(context.ChatId, "Я конечно искусственный интеллект, но этого не понял :)");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled bot's command");
                context.SetCurrentCommand(START_COMMAND);
                _metrics.Measure.Counter.Increment(BotMetrics.Errors);
                await _botClient.SendMessage(context.ChatId, "Что-то пошло не так, я уже занимаюсь проблемой!", replyMarkup: DEFAULT_KEYBOARD);
            }
        }

        private static ReplyKeyboardMarkup GetCounterKeyboard(List<Counter> counters)
        {
            var keyboard = counters.SelectMany(c => new List<List<KeyboardButton>> { new List<KeyboardButton> { new KeyboardButton($"{c.Title} - {c.Amount}") } }).ToList();
            keyboard.AddNewLineButton(BACK_COMMAND);
            return new ReplyKeyboardMarkup(keyboard) { ResizeKeyboard = true };
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Bot service stopped");
            return Task.CompletedTask;
        }
    }
}
