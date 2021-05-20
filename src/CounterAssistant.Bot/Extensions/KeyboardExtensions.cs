using System.Collections.Generic;
using Telegram.Bot.Types.ReplyMarkups;

namespace CounterAssistant.Bot.Extensions
{
    public static class KeyboardExtensions
    {
        public static void AddNewLineButton(this List<List<KeyboardButton>> keyboard, string buttonText)
        {
            keyboard.Add(new List<KeyboardButton> { new KeyboardButton(buttonText) });
        }
    }
}
