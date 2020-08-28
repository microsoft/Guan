using System;
using System.Globalization;
using System.Collections.Generic;

namespace Guan.Common
{
    public enum EventType
    {
        Error,
        Warning,
        Info,
        Info1,
        Info2,
        Verbose
    }

    internal interface IEventSink
    {
        void WriteEntry(string src, EventType msgType, string msgText);
    };

    public class EventLog
    {
        private class SinkWrapper
        {
            private IEventSink m_sink;
            private int m_defaultLevel;
            private int m_maxTraceLevel;

            private Dictionary<string, int> m_sourceOverride;

            public SinkWrapper(IEventSink sink, int defaultLevel, string sourceOverride)
            {
                m_sink = sink;
                m_defaultLevel = defaultLevel;
                m_maxTraceLevel = defaultLevel;

                if (!string.IsNullOrEmpty(sourceOverride))
                {
                    m_sourceOverride = new Dictionary<string, int>();

                    foreach (string entry in sourceOverride.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        int index = entry.IndexOf('=');
                        if (index > 0)
                        {
                            string key = entry.Substring(0, index).Trim();
                            string value = entry.Substring(index + 1).Trim();
                            int overrideLevel;
                            if (int.TryParse(value, out overrideLevel))
                            {
                                m_sourceOverride[key] = overrideLevel;
                                if (overrideLevel > m_maxTraceLevel)
                                {
                                    m_maxTraceLevel = overrideLevel;
                                }
                            }
                        }
                    }
                }
            }

            public IEventSink Sink
            {
                get
                {
                    return m_sink;
                }
            }

            public void WriteEntry(int msgLevel, string src, EventType msgType, string msgText, params object[] args)
            {
                if (msgLevel > m_maxTraceLevel)
                {
                    return;
                }

                int logLevel = m_defaultLevel;
                if (m_sourceOverride != null)
                {
                    string effectiveSrc = src;
                    while (!m_sourceOverride.TryGetValue(effectiveSrc, out logLevel))
                    {
                        int lastSegIndex = effectiveSrc.LastIndexOf('.');
                        if (lastSegIndex > 0)
                        {
                            effectiveSrc = effectiveSrc.Substring(0, lastSegIndex);
                        }
                        else
                        {
                            logLevel = m_defaultLevel;
                            break;
                        }
                    }
                }

                if (msgLevel <= logLevel)
                {
                    string outputText = string.Format(CultureInfo.InvariantCulture, msgText, args);
                    m_sink.WriteEntry(src, msgType, outputText);
                }
            }
        }

        private static List<SinkWrapper> s_sinks = CreateSinks(new List<SinkWrapper>());

        private static List<SinkWrapper> CreateSinks(List<SinkWrapper> current)
        {
            List<SinkWrapper> sinks = new List<SinkWrapper>();

            int consoleLevel = Utility.GetConfig("ConsoleSink.DefaultLevel", 3);
            if (consoleLevel >= 0)
            {
                sinks.Add(new SinkWrapper(ConsoleSink.Singleton, consoleLevel, Utility.GetConfig("ConsoleSink.Overrides")));
            }

            int fileLevel = Utility.GetConfig("FileSink.DefaultLevel", 4);
            if (fileLevel >= 0)
            {
                IEventSink fileSink = null;
                foreach (SinkWrapper sink in current)
                {
                    if (sink.Sink is FileEventSink)
                    {
                        fileSink = sink.Sink;
                    }
                }

                if (fileSink == null)
                {
                    fileSink = new FileEventSink("guan", null);
                }

                sinks.Add(new SinkWrapper(fileSink, fileLevel, Utility.GetConfig("FileSink.Overrides")));
            }

            return sinks;
        }

        public static void RefreshConfig()
        {
            s_sinks = CreateSinks(s_sinks);
        }

        public static void WriteError(string src, string format, params object[] args)
        {
            foreach (SinkWrapper sink in s_sinks)
            {
                sink.WriteEntry(1, src, EventType.Error, format, args);
            }
        }

        public static void WriteWarning(string src, string format, params object[] args)
        {
            foreach (SinkWrapper sink in s_sinks)
            {
                sink.WriteEntry(2, src, EventType.Warning, format, args);
            }
        }

        public static void WriteInfo(string src, string format, params object[] args)
        {
            foreach (SinkWrapper sink in s_sinks)
            {
                sink.WriteEntry(3, src, EventType.Info, format, args);
            }
        }

        public static void WriteInfo1(string src, string format, params object[] args)
        {
            foreach (SinkWrapper sink in s_sinks)
            {
                sink.WriteEntry(3, src, EventType.Info1, format, args);
            }
        }

        public static void WriteInfo2(string src, string format, params object[] args)
        {
            foreach (SinkWrapper sink in s_sinks)
            {
                sink.WriteEntry(3, src, EventType.Info2, format, args);
            }
        }

        public static void WriteVerbose(string src, string format, params object[] args)
        {
            foreach (SinkWrapper sink in s_sinks)
            {
                sink.WriteEntry(4, src, EventType.Verbose, format, args);
            }
        }
    }
}
