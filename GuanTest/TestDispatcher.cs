
using System;
using System.Collections.Generic;

namespace GuanTest
{
    /// <summary>
    /// The interface for test dispatcher that can work
    /// with TestSession.
    /// </summary>
    public interface ITestDispatcher
    {
        /// <summary>
        /// Open the test dispatcher for initialization.
        /// </summary>
        /// <param name="session">The session that owns the dispatcher</param>
        /// <returns>Whether the open is successful</returns>
        bool Open(TestSession session);

        /// <summary>
        /// Close the dispatcher.  Cleanup should be performed here.
        /// </summary>
        void Close();

        /// <summary>
        /// Reset the state of the dispatcher.
        /// </summary>
        void Reset();

        /// <summary>
        /// Execute test commands.  Each dispatcher should define
        /// its own commands.
        /// During the execution of the command, the dispatcher
        /// should throw an exception for error conditions.
        /// </summary>
        /// <param name="command">The test commands</param>
        /// <returns>Whether the command is valid</returns>
        bool ExecuteCommand(string command);
    }

    /// <summary>
    /// The base dispatcher that uses a TestVerifier for test
    /// verification.
    /// </summary>
    public abstract class TestDispatcher : ITestDispatcher
    {
        private TestSession m_session;
        private TestVerifier m_verifier;

        /// <summary>
        /// Constructor.
        /// </summary>
        protected TestDispatcher()
        {
            m_session = null;
            m_verifier = null;
        }

        /// <summary>
        /// The session that owns the dispatcher.
        /// </summary>
        public TestSession Session
        {
            get { return (m_session); }
        }

        /// <summary>
        /// The verifier associated with the dispatcher.
        /// </summary>
        protected TestVerifier Verifier
        {
            get { return (m_verifier); }
        }

        /// <summary>
        /// Open the test dispatcher for initialization.
        /// </summary>
        /// <param name="session">The session that owns the dispatcher</param>
        /// <returns>Whether the open is successful</returns>
        public virtual bool Open(TestSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }

            m_session = session;
            m_verifier = new TestVerifier(session.Label);

            return true;
        }

        /// <summary>
        /// Close the dispatcher.  Cleanup should be performed here.
        /// </summary>
        public virtual void Close()
        {
        }

        /// <summary>
        /// Reset the state of the dispatcher.
        /// </summary>
        public virtual void Reset()
        {
        }

        /// <summary>
        /// Intentionally terminate the session.
        /// </summary>
        protected virtual void Crash()
        {
            throw new IntendedException("intended crash");
        }

        /// <summary>
        /// Execute test commands.  Each dispatcher should define
        /// its own commands.
        /// During the execution of the command, the dispatcher
        /// should throw an exception for error conditions.
        /// </summary>
        /// <param name="command">The test commands</param>
        /// <returns>Whether the command is valid</returns>
        public abstract bool ExecuteCommand(string command);

        public void Report(object result)
        {
            Report(TestSession.ResultReport, "{0}", result);
        }

        public void Report(string format, params object[] args)
        {
            Report(TestSession.ResultReport, format, args);
        }

        /// <summary>
        /// Report information, including errors to the session.
        /// </summary>
        /// <param name="src">The source of the report.  This is used
        /// to determine what kind of report it is.  A list of sources
        /// is defined in TestSession but each dispatcher can define
        /// its own sources.
        /// An ErrorReport will be considered a test failure.
        /// </param>
        /// <param name="format">Format string</param>
        /// <param name="args">Arguments</param>
        public virtual void Report(string src, string format, params object[] args)
        {
            if (src == null)
            {
                throw new ArgumentNullException("src");
            }

            string data = string.Format(format, args);
            m_session.Report(src, "{0}", data);

            if (src.StartsWith(TestSession.ErrorReport))
            {
                m_verifier.ReportError(data);
            }
            else if (src == TestSession.ResultReport)
            {
                if (!m_verifier.ReportResult(data.Trim()))
                {
                    m_verifier.ReportError("Unexpected result: {0}", data);
                }
            }
        }

        /// <summary>
        /// Report events to the verifier.  If the event is not what
        /// the verifier is expecting, an error will be reported to
        /// the session.
        /// </summary>
        /// <param name="data">The event being reported.</param>
        /// <param name="desc">The error description that will be reported
        /// to the session if the event is not expected by the verifier.
        /// If null, such scenario won't be considered as error.
        /// </param>
        public void ReportToVerifier(string data, string desc)
        {
            if (m_verifier.ReportResult(data) == false && desc != null)
            {
                m_verifier.ReportError(desc + " => " + data);
            }
        }

        protected static List<string> ParseCommand(string command)
        {
            List<string> result = new List<string>();
            int start = -1;

            for (int i = 0; i < command.Length; i++)
            {
                if (char.IsWhiteSpace(command[i]))
                {
                    if (start >= 0)
                    {
                        result.Add(command.Substring(start, i - start));
                        start = -1;
                    }
                }
                else if (start < 0)
                {
                    start = i;

                    if (command[i] == '"' && i + 1 < command.Length)
                    {
                        int end = command.IndexOf('"', i + 1);
                        if (end > 0)
                        {
                            result.Add(command.Substring(i + 1, end - i - 1));
                            i = end;
                            start = -1;
                        }
                    }
                }
            }

            if (start >= 0)
            {
                result.Add(command.Substring(start));
            }

            return result;
        }
    }
}
