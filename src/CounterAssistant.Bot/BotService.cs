using CounterAssistant.Bot.Extensions;
using CounterAssistant.Bot.Flows;
using CounterAssistant.Domain.Models;
using System;
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<int, List<Counter>> _counters = new ConcurrentDictionary<int, List<Counter>>();

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

        public BotService(string token, IContextProvider contextProvider)
        {
            _botClient = new TelegramBotClient(token);
            _contextProvider = contextProvider;
            _counters = new ConcurrentDictionary<int, List<Counter>>();
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
                var context = await _contextProvider.GetContext(e.Message.From.Id);
                var message = e.Message.Text;
                var chatId = e.Message.Chat.Id;
                var userName = e.Message.From.FirstName;

                if (message == START_COMMAND)
                {
                    await _botClient.SendTextMessageAsync(chatId, $"Привет {userName}, меня зовут Джарвис, я счётчик-бот и хочу облегчить тебе жизнь!", replyMarkup: DEFAULT_KEYBOARD);
                }
                else if (message == CREATE_COUNTER_COMMAND || context.Command == CREATE_COUNTER_COMMAND)
                {
                    context.Command = CREATE_COUNTER_COMMAND;

                    if (context.CreateCounterFlow == null) context.CreateCounterFlow = new CreateCounterFlow();
                    var result = context.CreateCounterFlow.Perform(message);

                    if (result.IsSuccess)
                    {
                        context.Command = null;
                        context.CreateCounterFlow = null;
                        if (_counters.TryGetValue(context.UserId, out var counters))
                        {
                            counters.Add(result.Counter);
                        }
                        else
                        {
                            _counters[context.UserId] = new List<Counter> { result.Counter };
                        }

                        await _botClient.SendTextMessageAsync(chatId, result.Message, parseMode: ParseMode.Html, replyMarkup: GetCounterKeyboard(context.UserId));
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(chatId, result.Message);
                    }
                }
                else if(message == RESET_COUNTER_COMMAND)
                {
                    // todo
                    await _botClient.SendTextMessageAsync(chatId, text: "Эта фича еще в разработке", replyMarkup: DEFAULT_KEYBOARD);
                }
                else if(message == DISPLAY_ALL_COUNTERS_COMMAND)
                {
                    context.Command = SELECT_COUNTER_COMMAND;
                    await _botClient.SendTextMessageAsync(chatId, "Ваши счётчики:", replyMarkup: GetCounterKeyboard(context.UserId));
                }
                else if(message == BACK_COMMAND)
                {
                    if (context.Command == SELECT_COUNTER_COMMAND)
                    {
                        context.Command = null;
                        await _botClient.SendTextMessageAsync(chatId, "Выбирите действие:", replyMarkup: DEFAULT_KEYBOARD);
                    }
                    else if(context.Command == MANAGE_COUNTER_COMMAND)
                    {
                        context.Command = SELECT_COUNTER_COMMAND;
                        await _botClient.SendTextMessageAsync(chatId, "Ваши счётчики:", replyMarkup: GetCounterKeyboard(context.UserId));
                    }
                }
                else if(context.Command == SELECT_COUNTER_COMMAND)
                {
                    var counterName = message.Substring(0, message.IndexOf(" -"));
                    var counter = _counters[context.UserId].FirstOrDefault(c => c.Title == counterName);
                    context.EditCounterFlow = new EditCounterFlow(counter);

                    context.Command = MANAGE_COUNTER_COMMAND;

                    await _botClient.SendTextMessageAsync(chatId, $"<b>Счётчик {counter.Title} - {counter.Amount}</b>", parseMode: ParseMode.Html, replyMarkup: COUNTER_KEYBOARD);
                }
                else if(message == DECREMENT_COMMAND)
                {
                    context.EditCounterFlow.Counter.Decrement();
                    await _botClient.SendTextMessageAsync(chatId, $"Счётчик успешно уменьшен: <b>{context.EditCounterFlow.Counter.Title} - {context.EditCounterFlow.Counter.Amount}</b>", parseMode: ParseMode.Html);
                }
                else if(message == INCREMENT_COMMAND)
                {
                    context.EditCounterFlow.Counter.Increment();
                    await _botClient.SendTextMessageAsync(chatId, $"Счётчик успешно увеличен: <b>{context.EditCounterFlow.Counter.Title} - {context.EditCounterFlow.Counter.Amount}</b>", parseMode: ParseMode.Html);
                }
                else
                {
                }
            }
        }
        private ReplyKeyboardMarkup GetCounterKeyboard(int userId)
        {
            if (_counters.TryGetValue(userId, out var counters))
            {
                var keyboard = counters.SelectMany(c => new List<List<KeyboardButton>> { new List<KeyboardButton> { new KeyboardButton($"{c.Title} - {c.Amount}") } }).ToList();
                keyboard.AddNewLineButton(BACK_COMMAND);
                return new ReplyKeyboardMarkup(keyboard) { ResizeKeyboard = true };
            }

            return null;
        }

        public void Dispose()
        {
            _botClient?.StopReceiving();
        }
    }
}
