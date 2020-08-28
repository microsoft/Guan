using System;
using System.IO;
using System.Globalization;
using System.Threading;

namespace Guan.Common
{
    internal sealed class FileEventSink : IEventSink
    {
        private TextWriter m_fs;
        private string m_fileName;
        private string m_formatString;
        private string m_logName;

        public FileEventSink(string logName, string fileNameFormat)
        {
            m_logName = logName;
            m_formatString = fileNameFormat;
        }

        private void cleanup()
        {
            if (m_fs != null)
            {
                try
                {
                    m_fs.Close();
                }
                catch (IOException)
                {
                }
                catch (ObjectDisposedException)
                {
                }

                m_fs = null;
            }
        }

        public void WriteEntry(string src, EventType msgType, string msgText)
        {
            if (msgText == null)
            {
                return;
            }

            lock (this)
            {
                if (m_fs == null || m_formatString != null)
                {
                    string newFileName;
                    if (m_formatString != null)
                    {
                        newFileName = m_logName + DateTime.Now.ToString(m_formatString, CultureInfo.InvariantCulture) + ".trace";
                        if (newFileName != m_fileName)
                        {
                            cleanup();
                            m_fileName = newFileName;
                        }
                    }
                    else
                    {
                        newFileName = m_logName + ".trace";
                    }

                    if (m_fs == null)
                    {
                        m_fs = new StreamWriter(File.Open(newFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                    }
                }

                msgText = msgText.Replace("\n", "\t");
                try
                {
                    m_fs.WriteLine("{0},{1},{2},{3},{4}", Utility.FormatTime(DateTime.Now), msgType, Thread.CurrentThread.ManagedThreadId, src, msgText);
                    m_fs.Flush();
                }
                catch (IOException)
                {
                }
            }
        }
    }
}
