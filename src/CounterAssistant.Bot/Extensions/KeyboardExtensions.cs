using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types.ReplyMarkups;

namespace CounterAssistant.Bot.Extensions
{
    public static class KeyboardExtensions
    {
        public static void AddNewLineButton(this List<List<KeyboardButton>> keyboard, string buttonText)
        {
            keyboard.Add(new List<KeyboardButton> { new KeyboardButton(buttonText) });
        }

        public static InlineKeyboardMarkup ToInlineButtons(this Dictionary<string, string> buttons)
        {
            return new InlineKeyboardMarkup(buttons.Select(kvp => new InlineKeyboardButton { CallbackData = kvp.Key, Pay = false, Text = kvp.Value }));
        }


        //U+ should be replaced by 0x
        public static InlineKeyboardMarkup ToPaymentButton(string action, string text, string url)
        {
            return new InlineKeyboardMarkup(new InlineKeyboardButton { Url = url , CallbackData = action, Pay = false, Text = $"{char.ConvertFromUtf32(0x27A1)} {text}" });
        }
    }
}
