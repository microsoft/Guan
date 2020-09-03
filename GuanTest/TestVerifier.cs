
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Guan.Common;

namespace GuanTest
{
    /// <summary>
    /// The class to verify that expected events occurred.
    /// Each event is simply modeled as a string.
    /// </summary>
    public class TestVerifier
    {
        /// <summary>
        /// The name of the verifier.  This is used in the output
        /// of the verifier.
        /// </summary>
        private string m_name;

        /// <summary>
        /// The expected events that have not been reported. 
        /// </summary>
        private List<string> m_pending;

        /// <summary>
        /// The errors reported.
        /// </summary>
        private List<string> m_errors;

        /// <summary>
        /// The interval for the verifier to check whether all expected
        /// events have happened when Wait is called.
        /// </summary>
        private static readonly TimeSpan s_waitInterval = new TimeSpan(0, 0, 1);

        private bool m_active;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The name of the verifier</param>
        public TestVerifier(string name)
        {
            m_name = name;
            m_pending = new List<string>();
            m_errors = new List<string>();
            m_active = false;
        }

        /// <summary>
        /// Whether all expected events have been reported with no
        /// error.
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                lock (this)
                {
                    return (m_pending.Count == 0 && m_errors.Count == 0);
                }
            }
        }

        /// <summary>
        /// Report an error to the verifier.
        /// </summary>
        /// <param name="format">Format string</param>
        /// <param name="args">Arguments</param>
        public void ReportError(string format, params object[] args)
        {
            EventLog.WriteError("TestVerifier", format, args);

            lock (this)
            {
                m_errors.Add(string.Format(format, args));
            }
        }

        /// <summary>
        /// The concatenation of the errors reported.  If there
        /// has been no error reported, null will be returned.
        /// </summary>
        public string Errors
        {
            get
            {
                string result = null;
                lock (this)
                {
                    foreach (string error in m_errors)
                        result += (error + "\n");
                }

                return result;
            }
        }

        /// <summary>
        /// The concatenation of the pending events.  If there is
        /// no pending event, empty string is returned.
        /// </summary>
        public string Pending
        {
            get
            {
                string result = "";
                lock (this)
                {
                    foreach (string s in m_pending)
                        result += (s + " ");
                }

                return result.Trim();
            }
        }

        /// <summary>
        /// Clear any pending events and errors.
        /// </summary>
        public void Clear()
        {
            lock (this)
            {
                m_pending.Clear();
                m_errors.Clear();
            }
        }

        /// <summary>
        /// Report an event.  If the event is an expected one, it
        /// will be removed from the pending list.
        /// </summary>
        /// <param name="data">The reported event</param>
        /// <returns>Whether the reported event is an expected
        /// pending event.</returns>
        public bool ReportResult(string data)
        {
            bool result;

            lock (this)
            {
                result = m_pending.Remove(data);
            }

            return result || !m_active;
        }

        /// <summary>
        /// Remove pending results that matches the specified pattern.
        /// </summary>
        /// <param name="pattern">Pattern specified.</param>
        public void Remove(string pattern)
        {
            Regex regex = new Regex(pattern, RegexOptions.Compiled);

            lock (this)
            {
                for (int i = m_pending.Count - 1; i >= 0; i--)
                {
                    if (regex.IsMatch(m_pending[i]))
                    {
                        m_pending.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Add an expected event to the pending list.
        /// </summary>
        /// <param name="data">The expected event</param>
        public void Expect(string data)
        {
            lock (this)
            {
                m_pending.Add(data);
                m_active = true;
            }
        }

        /// <summary>
        /// Add a collection of expected events to the pending list.
        /// </summary>
        /// <param name="data">The collection of pending events</param>
        public void Expect(IEnumerable<string> data)
        {
            foreach (string s in data)
            {
                Expect(s);
            }
        }

        /// <summary>
        /// Wait until all expected events are reported or an error is
        /// reported.
        /// </summary>
        /// <param name="timeout">The time to wait</param>
        public void Wait(TimeSpan timeout)
        {
            m_active = false;

            DateTime expireTime = DateTime.Now.Add(timeout);

            bool done = false;
            while (!done)
            {
                if (IsCompleted)
                {
                    return;
                }

                string errors = Errors;
                if (errors != null)
                {
                    ReleaseAssert.Fail("{0} failed: {1}\nPending: {2}", m_name, errors, Pending);
                }

                TimeSpan remainTime = expireTime.Subtract(DateTime.Now);
                if (remainTime > s_waitInterval)
                {
                    remainTime = s_waitInterval;
                }

                if (remainTime > TimeSpan.Zero)
                {
                    Thread.Sleep(remainTime);
                }
                else
                {
                    done = true;
                }
            }

            string result = Pending;
            if (result.Length > 0)
            {
                ReleaseAssert.Fail("{0} failed.  Pending results: {1}", m_name, result);
            }
        }
    }
}
