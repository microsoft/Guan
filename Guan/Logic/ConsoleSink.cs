// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Collections.Generic;

    internal static class ConsoleSink
    {
        private static ConsoleColor oldColor = Console.ForegroundColor;
        private static Dictionary<ConsoleColor, ConsoleColor> colorMapping = GetColorMapping();

        public static int GetColor(ConsoleColor color)
        {
            int bc = (int)Console.BackgroundColor;
            bool bDark = (bc <= 7);

            int fc = (int)color;
            if (bDark)
            {
                fc |= 0x08;
            }
            else
            {
                fc &= 0x07;
            }

            return fc;
        }

        public static void WriteLine(int color, string text)
        {
            lock (colorMapping)
            {
                if (color >= 0)
                {
                    ConsoleColor msgColor = (ConsoleColor)color;
                    if (msgColor != oldColor)
                    {
                        Console.ForegroundColor = msgColor;
                    }

                    try
                    {
                        Console.WriteLine(text);
                    }
                    finally
                    {
                        if (msgColor != oldColor)
                        {
                            Console.ForegroundColor = oldColor;
                        }
                    }
                }
                else
                {
                    Console.WriteLine(text);
                }
            }
        }

        public static void WriteLine(ConsoleColor color, string format, params object[] args)
        {
            ConsoleColor msgColor;
            if (!colorMapping.TryGetValue(color, out msgColor))
            {
                msgColor = color;
            }

            WriteLine((int)msgColor, string.Format(format, args));
        }

        private static Dictionary<ConsoleColor, ConsoleColor> GetColorMapping()
        {
            Dictionary<ConsoleColor, ConsoleColor> map = new Dictionary<ConsoleColor, ConsoleColor>();
            AddColor(map, ConsoleColor.Red);
            AddColor(map, ConsoleColor.Yellow);
            AddColor(map, ConsoleColor.Cyan);
            AddColor(map, ConsoleColor.Green);

            return map;
        }

        private static void AddColor(Dictionary<ConsoleColor, ConsoleColor> map, ConsoleColor color)
        {
            map[color] = (ConsoleColor)GetColor(color);
        }
    }
}
