using System.Diagnostics.CodeAnalysis;

namespace CounterAssistant.Bot
{
    [ExcludeFromCodeCoverage]
    public static class Emoji
    {
        public static string DownArrow => char.ConvertFromUtf32(0x2B07);
        public static string RightArrow => char.ConvertFromUtf32(0x27A1);
    }
}
