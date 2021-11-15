// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;

    public abstract class EventLogWriter
    {
        private static EventLogWriter current = new ConsoleWriter();

        protected enum LogLevel
        {
            Trace = 0,
            Debug = 1,
            Information = 2,
            Warning = 3,
            Error = 4,
        }

        public static void Set(EventLogWriter writer)
        {
            current = writer;
        }

        public static void WriteInfo(string format, params object[] args)
        {
            current.WriteEntry(LogLevel.Information, format, args);
        }

        public static void WriteWarning(string format, params object[] args)
        {
            current.WriteEntry(LogLevel.Warning, format, args);
        }

        public static void WriteError(string format, params object[] args)
        {
            current.WriteEntry(LogLevel.Error, format, args);
        }

        protected abstract void WriteEntry(LogLevel level, string format, params object[] args);

        private class ConsoleWriter : EventLogWriter
        {
            protected override void WriteEntry(LogLevel level, string format, params object[] args)
            {
                int color;
                switch (level)
                {
                    case LogLevel.Error:
                        color = (int)ConsoleColor.Red;
                        break;
                    case LogLevel.Warning:
                        color = (int)ConsoleColor.Yellow;
                        break;
                    default:
                        color = -1;
                        break;
                }

                ConsoleSink.WriteLine(color, string.Format(format, args));
            }
        }
    }
}
