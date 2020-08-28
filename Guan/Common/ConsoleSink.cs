using System;
using System.Collections.Generic;

namespace Guan.Common
{
    public class ConsoleSink : IEventSink
    {
        public static ConsoleSink Singleton = new ConsoleSink();
        private static ConsoleColor s_oldColor = Console.ForegroundColor;
        private static Dictionary<EventType, ConsoleColor> s_colorMapping = GetColorMapping();

        private static Dictionary<EventType, ConsoleColor> GetColorMapping()
        {
            Dictionary<EventType, ConsoleColor> map = new Dictionary<EventType, ConsoleColor>();
            AddColor(map, EventType.Error, ConsoleColor.Red);
            AddColor(map, EventType.Warning, ConsoleColor.Yellow);
            AddColor(map, EventType.Info1, ConsoleColor.Cyan);
            AddColor(map, EventType.Info2, ConsoleColor.Green);

            return map;
        }

        private static void AddColor(Dictionary<EventType, ConsoleColor> map,
                                     EventType key, ConsoleColor color)
        {
            int bc = (int) Console.BackgroundColor;
            bool bDark = (bc <= 7);

            int fc = (int) color;
            if (bDark)
            {
                fc |= 0x08;
            }
            else
            {
                fc &= 0x07;
            }

            map[key] = (ConsoleColor) fc;
        }

        private ConsoleSink()
        {
        }

        public void WriteEntry(string src, EventType msgType, string msgText)
        {
            lock (s_colorMapping)
            {
                ConsoleColor msgColor = s_oldColor;
                try
                {
                    if (s_colorMapping.TryGetValue(msgType, out msgColor))
                    {
                        Console.ForegroundColor = msgColor;
                    }

                    Console.WriteLine(msgText);
                }
                finally
                {
                    if (msgColor != s_oldColor)
                    {
                        Console.ForegroundColor = s_oldColor;
                    }
                }
            }
        }
    }
}
