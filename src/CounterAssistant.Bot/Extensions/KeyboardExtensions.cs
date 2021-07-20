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
            var number = 4;

            var lines = new List<IEnumerable<InlineKeyboardButton>>();

            for(var i = 0; i < buttons.Count; i += number)
            {
                var line = buttons.Skip(i).Take(number).ToArray().Select(kvp => new InlineKeyboardButton { CallbackData = kvp.Key, Pay = false, Text = kvp.Value });
                lines.Add(line);
            }

            return new InlineKeyboardMarkup(lines);
        }

        //U+ should be replaced by 0x
        public static InlineKeyboardMarkup ToPaymentButton(string action, string text, string url)
        {
            return new InlineKeyboardMarkup(new InlineKeyboardButton { Url = url , CallbackData = action, Pay = false, Text = $"{char.ConvertFromUtf32(0x27A1)} {text}" });
        }
    }
}
