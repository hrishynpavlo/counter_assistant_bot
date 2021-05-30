﻿using App.Metrics;
using CounterAssistant.Bot.Extensions;
using CounterAssistant.DataAccess;
using CounterAssistant.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                    new KeyboardButton(SETTINGS_COMMAND)
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

        public BotService(ITelegramBotClient botClient, IContextProvider contextProvider, ICounterStore store, ILogger<BotService> logger, IMetricsRoot metrics)
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

                var context = await _contextProvider.GetContextAsync(e.Message);
                var message = e.Message.Text;

                try
                {
                    if (message == START_COMMAND)
                    {
                        context.SetCurrentCommand(START_COMMAND);
                        _logger.LogInformation("User {user} has started bot", context.UserId);
                        await _botClient.SendTextMessageAsync(context.ChatId, $"Привет {context.Name}, меня зовут Джарвис, я счётчик-бот и хочу облегчить тебе жизнь!", replyMarkup: DEFAULT_KEYBOARD);
                    }
                    else if (message == CREATE_COUNTER_COMMAND || context.Command == CREATE_COUNTER_COMMAND)
                    {
                        context.SetCurrentCommand(CREATE_COUNTER_COMMAND);

                        if (context.CreateCounterFlow == null) context.StartCreateCounterFlow();
                        var result = context.CreateCounterFlow.Perform(message);

                        if (!result.IsSuccess)
                        {
                            await _botClient.SendTextMessageAsync(context.ChatId, result.Message);
                        }
                        else
                        {
                            context.SetCurrentCommand(SELECT_COUNTER_COMMAND);
                            context.FinishCreateCounterFlow();

                            await _store.CreateCounterAsync(result.Counter, context.UserId);
                            _logger.LogInformation("user {user} has successfully created counter {id}", context.UserId, result.Counter.Id);

                            var counters = await _store.GetCountersByUserIdAsync(context.UserId);

                            await _botClient.SendTextMessageAsync(context.ChatId, result.Message, parseMode: ParseMode.Html, replyMarkup: GetCounterKeyboard(counters));
                        }
                    }
                    else if (message == SETTINGS_COMMAND)
                    {
                        // todo
                        await _botClient.SendTextMessageAsync(context.ChatId, text: "Эта фича еще в разработке", replyMarkup: DEFAULT_KEYBOARD);
                    }
                    else if (message == DISPLAY_ALL_COUNTERS_COMMAND)
                    {
                        var counters = await _store.GetCountersByUserIdAsync(context.UserId);
                        context.SetCurrentCommand(SELECT_COUNTER_COMMAND);
                        await _botClient.SendTextMessageAsync(context.ChatId, text: GetCountersMessage(counters), replyMarkup: GetCounterKeyboard(counters));
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
                            var counters = await _store.GetCountersByUserIdAsync(context.UserId);
                            await _botClient.SendTextMessageAsync(context.ChatId, "Ваши счётчики:", replyMarkup: GetCounterKeyboard(counters));
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
                        context.SelectCounter(counter);

                        context.SetCurrentCommand(MANAGE_COUNTER_COMMAND);

                        await _botClient.SendTextMessageAsync(context.ChatId, text: GetCounterMessage(context.SelectedCounter), parseMode: ParseMode.Html, replyMarkup: COUNTER_KEYBOARD);
                    }
                    else if (message == DECREMENT_COMMAND)
                    {
                        context.SelectedCounter.Decrement();
                        //todo: really safety async update
                        _store.UpdateAsync(context.SelectedCounter);
                        await _botClient.SendTextMessageAsync(context.ChatId, $"Счётчик успешно уменьшен: <b>{context.SelectedCounter.Title}: {context.SelectedCounter.Amount}</b>", parseMode: ParseMode.Html);
                    }
                    else if (message == INCREMENT_COMMAND)
                    {
                        context.SelectedCounter.Increment();
                        _store.UpdateAsync(context.SelectedCounter);
                        await _botClient.SendTextMessageAsync(context.ChatId, $"Счётчик успешно увеличен: <b>{context.SelectedCounter.Title}: {context.SelectedCounter.Amount}</b>", parseMode: ParseMode.Html);
                    }
                    else
                    {
                        _logger.LogInformation("user {id} message: {msg} is not recognized as a bot command", context.UserId, message);
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Unhandled bot's command");
                    context.SetCurrentCommand(START_COMMAND);
                    await _botClient.SendTextMessageAsync(context.ChatId, "Что-то пошло не так, я уже занимаюсь проблемой!", replyMarkup: DEFAULT_KEYBOARD);
                }
            }
        }

        private string GetCounterMessage(Counter counter)
        {
            return $"<b>Счётчик {counter.Title.ToUpper()}\nШаг: {counter.Step}\nСоздан: {counter.CreatedAt}\nОбновлен последний раз: {counter.LastModifiedAt}\nРежим: {(counter.IsManual ? "ручной" : "автоматический")}</b>\n";
        }

        private string GetCountersMessage(List<Counter> counters)
        {
            var sb = new StringBuilder();
            counters.ForEach(x => sb.AppendLine(GetCounterMessage(x)));
            return sb.ToString();
        }

        private ReplyKeyboardMarkup GetCounterKeyboard(List<Counter> counters)
        {
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
