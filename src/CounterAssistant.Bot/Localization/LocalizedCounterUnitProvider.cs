using CounterAssistant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CounterAssistant.Bot.Localization
{
    public class LocalizedCounterUnitProvider
    {
        private static HashSet<int> _exclusions = Enumerable.Range(11, 10).ToHashSet(); // 11 - 20
        private static HashSet<int> _plural2_4 = Enumerable.Range(2, 3).ToHashSet(); // 2 - 4
        private static HashSet<int> _plural5_9 = Enumerable.Range(5, 5).ToHashSet(); // 5 - 9

        public static string GetUnitForm(CounterUnit unit, int amount)
        {
            if (amount % 10 == 1 && !_exclusions.Contains(amount)) // modulo 1
            {
                return unit switch
                {
                    CounterUnit.Day => "День",
                    CounterUnit.Evening => "Вечер",
                    CounterUnit.Lesson => "Урок",
                    CounterUnit.Morning => "Утро",
                    CounterUnit.Time => "Раз",
                    CounterUnit.Training => "Тренировка",
                    CounterUnit.Week => "Неделя",
                    _ => throw new ArgumentOutOfRangeException(nameof(unit))
                };
            }
            else if (_plural2_4.Contains(amount % 10) && !_exclusions.Contains(amount)) // modulo 2 - 4
            {
                return unit switch
                {
                    CounterUnit.Day => "Дня",
                    CounterUnit.Evening => "Вечера",
                    CounterUnit.Lesson => "Урока",
                    CounterUnit.Morning => "Утра",
                    CounterUnit.Time => "Раза",
                    CounterUnit.Training => "Тренировки",
                    CounterUnit.Week => "Недели",
                    _ => throw new ArgumentOutOfRangeException(nameof(unit))
                };
            }
            else if (_exclusions.Contains(amount) || amount % 10 == 0 || _plural5_9.Contains(amount % 10))
            {
                return unit switch
                {
                    CounterUnit.Day => "Дней",
                    CounterUnit.Evening => "Вечеров",
                    CounterUnit.Lesson => "Уроков",
                    CounterUnit.Morning => "Утро",
                    CounterUnit.Time => "Раз",
                    CounterUnit.Training => "Тренировов",
                    CounterUnit.Week => "Недель",
                    _ => throw new ArgumentOutOfRangeException(nameof(unit))
                };
            }
            else return unit.ToString();
        }

        public static Dictionary<string, string> Get()
        {
            return new Dictionary<string, string> 
            {
                [CounterUnit.Day.ToString()] = "День",
                [CounterUnit.Week.ToString()] = "Неделя",
                [CounterUnit.Lesson.ToString()] = "Урок",
                [CounterUnit.Time.ToString()] = "Раз",
                [CounterUnit.Training.ToString()] = "Тренировка",
                [CounterUnit.Evening.ToString()] = "Вечер",
                [CounterUnit.Morning.ToString()] = "Утро"
            };
        }
    }
}
