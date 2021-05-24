using App.Metrics;
using CounterAssistant.Bot.Extensions;
using CounterAssistant.Bot.Flows;
using CounterAssistant.DataAccess;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static CounterAssistant.Bot.BotCommands;

namespace CounterAssistant.Bot
{
    public class BotService : IDisposable
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IContextProvider _contextProvider;
        private readonly ICounterStore _store;
        private readonly ILogger<BotService> _logger;
        private readonly IMetricsRoot _metrics;

        private readonly static ReplyKeyboardMarkup DEFAULT_KEYBOARD = new ReplyKeyboardMarkup
        {
            Keyboard = new List<List<KeyboardButton>>
            {
                new List<KeyboardButton>
                {
                    new KeyboardButton(CREATE_COUNTER_COMMAND),
                    new KeyboardButton(RESET_COUNTER_COMMAND)
                },
                new List<KeyboardButton>
                {
                    new KeyboardButton(DISPLAY_ALL_COUNTERS_COMMAND)
                }
            },
            ResizeKeyboard = true
        };
        private readonly static ReplyKeyboardMarkup COUNTER_KEYBOARD = new ReplyKeyboardMarkup
        {
            Keyboard = new List<List<KeyboardButton>>
            {
                new List<KeyboardButton>
                {
                    new KeyboardButton(DECREMENT_COMMAND),
                    new KeyboardButton(INCREMENT_COMMAND),
                },
                new List<KeyboardButton>
                {
                    new KeyboardButton(BACK_COMMAND)
                }
            },
            ResizeKeyboard = true
        };

        public BotService(TelegramBotClient botClient, IContextProvider contextProvider, ICounterStore store, ILogger<BotService> logger, IMetricsRoot metrics)
        {
            _botClient = botClient;
            _contextProvider = contextProvider;
            _store = store;
            _logger = logger;
            _metrics = metrics;
        }

        public async Task StartAsync()
        {
            _botClient.OnMessage += MessageHandler;

            var commands = new[]
            {
                new BotCommand { Command = START_COMMAND, Description = "старт" }
            };

            await _botClient.SetMyCommandsAsync(commands);

            _botClient.StartReceiving();
        }

        public async void MessageHandler(object sender, MessageEventArgs e)
        {
            if (e.Message.Text != null)
            {
                _metrics.Measure.Counter.Increment(BotMetrics.RecievedMessages);

                var context = await _contextProvider.GetContext(e.Message);

                try
                {
                    var message = e.Message.Text;

                    if (message == START_COMMAND)
                    {
                        context.SetCurrentCommand(START_COMMAND);
                        _logger.LogInformation("user {user} expose {command}", context.UserId, context.Command);
                        await _botClient.SendTextMessageAsync(context.ChatId, $"Привет {context.Name}, меня зовут Джарвис, я счётчик-бот и хочу облегчить тебе жизнь!", replyMarkup: DEFAULT_KEYBOARD);
                    }
                    else if (message == CREATE_COUNTER_COMMAND || context.Command == CREATE_COUNTER_COMMAND)
                    {
                        context.SetCurrentCommand(CREATE_COUNTER_COMMAND);

                        if (context.CreateCounterFlow == null) context.CreateCounterFlow = new CreateCounterFlow();
                        var result = context.CreateCounterFlow.Perform(message);

                        if (result.IsSuccess)
                        {
                            context.SetCurrentCommand(SELECT_COUNTER_COMMAND);
                            context.CreateCounterFlow = null;

                            await _store.CreateCounterAsync(result.Counter, context.UserId);
                            _logger.LogInformation("counter {id} successfully created", result.Counter.Id);

                            await _botClient.SendTextMessageAsync(context.ChatId, result.Message, parseMode: ParseMode.Html, replyMarkup: await GetCounterKeyboard(context.UserId));
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(context.ChatId, result.Message);
                        }
                    }
                    else if (message == RESET_COUNTER_COMMAND)
                    {
                        // todo
                        await _botClient.SendTextMessageAsync(context.ChatId, text: "Эта фича еще в разработке", replyMarkup: DEFAULT_KEYBOARD);
                    }
                    else if (message == DISPLAY_ALL_COUNTERS_COMMAND)
                    {
                        context.SetCurrentCommand(SELECT_COUNTER_COMMAND);
                        await _botClient.SendTextMessageAsync(context.ChatId, "Ваши счётчики:", replyMarkup: await GetCounterKeyboard(context.UserId));
                    }
                    else if (message == BACK_COMMAND)
                    {
                        if (context.Command == SELECT_COUNTER_COMMAND)
                        {
                            context.SetCurrentCommand(START_COMMAND);
                            await _botClient.SendTextMessageAsync(context.ChatId, "Выбирите действие:", replyMarkup: DEFAULT_KEYBOARD);
                        }
                        else if (context.Command == MANAGE_COUNTER_COMMAND)
                        {
                            context.SetCurrentCommand(SELECT_COUNTER_COMMAND);
                            //context.EditCounterFlow = null;
                            await _botClient.SendTextMessageAsync(context.ChatId, "Ваши счётчики:", replyMarkup: await GetCounterKeyboard(context.UserId));
                        }
                        else
                        {
                            context.SetCurrentCommand(START_COMMAND);
                            await _botClient.SendTextMessageAsync(context.ChatId, "Выбирите действие:", replyMarkup: DEFAULT_KEYBOARD);
                        }
                    }
                    else if (context.Command == SELECT_COUNTER_COMMAND)
                    {
                        var counterName = message.Substring(0, message.IndexOf(" -"));
                        var counter = await _store.GetCounterByNameAsync(context.UserId, counterName);
                        context.EditCounterFlow = new EditCounterFlow(counter);

                        context.SetCurrentCommand(MANAGE_COUNTER_COMMAND);

                        await _botClient.SendTextMessageAsync(context.ChatId, $"<b>Счётчик {counter.Title} - {counter.Amount}</b>\nШаг: {counter.Step}\nОбновлен последний раз: {counter.LastModifiedAt}", parseMode: ParseMode.Html, replyMarkup: COUNTER_KEYBOARD);
                    }
                    else if (message == DECREMENT_COMMAND)
                    {
                        context.EditCounterFlow.Counter.Decrement();
                        _store.UpdateAsync(context.EditCounterFlow.Counter);
                        await _botClient.SendTextMessageAsync(context.ChatId, $"Счётчик успешно уменьшен: <b>{context.EditCounterFlow.Counter.Title} - {context.EditCounterFlow.Counter.Amount}</b>", parseMode: ParseMode.Html);
                    }
                    else if (message == INCREMENT_COMMAND)
                    {
                        context.EditCounterFlow.Counter.Increment();
                        _store.UpdateAsync(context.EditCounterFlow.Counter);
                        await _botClient.SendTextMessageAsync(context.ChatId, $"Счётчик успешно увеличен: <b>{context.EditCounterFlow.Counter.Title} - {context.EditCounterFlow.Counter.Amount}</b>", parseMode: ParseMode.Html);
                    }
                    else
                    {
                        _logger.LogInformation("user {id} message: '{msg}' is not recognized", context.UserId, message);
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Unhandled message for bot!");
                    context.SetCurrentCommand(START_COMMAND);
                    await _botClient.SendTextMessageAsync(e.Message.Chat.Id, "Что-то пошло не так, я уже занимаюсь проблемой!", replyMarkup: DEFAULT_KEYBOARD);
                }
            }
        }

        private async Task<ReplyKeyboardMarkup> GetCounterKeyboard(int userId)
        {
            var counters = await _store.GetCountersByUserIdAsync(userId);
            var keyboard = counters.SelectMany(c => new List<List<KeyboardButton>> { new List<KeyboardButton> { new KeyboardButton($"{c.Title} - {c.Amount}") } }).ToList();
            keyboard.AddNewLineButton(BACK_COMMAND);
            return new ReplyKeyboardMarkup(keyboard) { ResizeKeyboard = true };
        }

        public void Dispose()
        {
            _botClient?.StopReceiving();
        }
    }
}
