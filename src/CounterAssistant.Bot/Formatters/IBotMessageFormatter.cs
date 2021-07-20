using CounterAssistant.Bot.Localization;
using CounterAssistant.Domain.Models;
using System.Collections.Generic;
using System.Text;

namespace CounterAssistant.Bot.Formatters
{
    public interface IBotMessageFormatter
    {
        string GetDetailedCounter(Counter counter);
        string GetDetailedCounters(IEnumerable<Counter> counters);
    }

    public class BotMessageFormatter : IBotMessageFormatter
    {
        public string GetDetailedCounter(Counter counter)
        {
            return $"{Emoji.RightArrow} <b>Счётчик:</b> {counter.Title.ToUpper()} \n" +
                   $"<b>Значение:</b> {counter.Amount} {LocalizedCounterUnitProvider.GetUnitForm(counter.Unit, counter.Amount)} \n" +
                   $"<b>Шаг:</b> {counter.Step} \n" +
                   $"<b>Тип:</b> {GetType(counter.IsManual)} \n" +
                   $"<b>Создан:</b> {counter.CreatedAt} \n" +
                   $"<b>Обновлен последний раз:</b> {counter.LastModifiedAt} \n";
        }
        public string GetDetailedCounters(IEnumerable<Counter> counters)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"{Emoji.DownArrow} <b>ВАШИ СЧЁТЧИКИ</b> {Emoji.DownArrow}\n");

            foreach(var counter in counters)
            {
                sb.AppendLine(GetDetailedCounter(counter));
            }

            return sb.ToString();
        }

        private static string GetType(bool isManual) => isManual ? "Ручной" : "Автоматический";
    }
}
