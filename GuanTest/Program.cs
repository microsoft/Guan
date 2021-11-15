using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Guan.Logic;

namespace GuanTest
{
    class Program
    {
        class TestContext
        {
            private List<string> tests_;
            private List<string> pending_;
            private int threadCount_;
            private int next_;
            private int passed_;
            private int failed_;
            private StringBuilder failedTests_;
            private AutoResetEvent completedEvent_;

            public TestContext(List<string> tests, int threadCount)
            {
                tests_ = tests;
                pending_ = new List<string>(tests_);
                threadCount_ = threadCount;
                next_ = 0;
                passed_ = failed_ = 0;
                failedTests_ = new StringBuilder(": (");
                completedEvent_ = new AutoResetEvent(false);
            }

            public int Run()
            {
                if (tests_.Count == 1)
                {
                    return RunTest(tests_[0]);
                }

                DateTime t1 = DateTime.UtcNow;

                for (int i = 0; i < threadCount_; i++)
                {
                    Task.Run(() => { RunThread(); });
                }

                completedEvent_.WaitOne();

                failedTests_.Append(")");
                DateTime t2 = DateTime.UtcNow;
                Console.WriteLine();
                Console.WriteLine("Passed {0}, failed {1}{2}, time spent {3}",
                    passed_, failed_, failed_ > 0 ? failedTests_.ToString() : "", t2 - t1);

                return failed_ > 0 ? -1 : 0;
            }

            public void RunThread()
            {
                int last = -1;
                int result = 0;

                while (true)
                {
                    lock (this)
                    {
                        if (last >= 0)
                        {
                            if (result == 0)
                            {
                                passed_++;
                            }
                            else
                            {
                                if (failed_ > 0)
                                {
                                    failedTests_.Append(",");
                                }
                                failedTests_.Append(tests_[last]);
                                failed_++;
                            }

                            pending_.Remove(tests_[last]);
                        }

                        if (next_ >= tests_.Count)
                        {
                            threadCount_--;
                            if (threadCount_ == 0)
                            {
                                completedEvent_.Set();
                            }

                            EventLogWriter.WriteInfo("Thread completed, remaining: {0}", Utility.CollectionToString(pending_));
                            return;
                        }

                        last = next_;
                        next_++;
                    }

                    EventLogWriter.WriteInfo("Start running {0}", tests_[last]);
                    result = RunTest(tests_[last]);
                    EventLogWriter.WriteInfo("{0} result {1}", tests_[last], result);
                }
            }
        }

        static int RunTest(string name)
        {
            GuanTestSession session = new GuanTestSession();
            session.Load(name);

            DateTime t1 = DateTime.UtcNow;
            int result = session.Execute();
            DateTime t2 = DateTime.UtcNow;

            EventLogWriter.WriteInfo("{0} time spent: {1}, result = {2}", name, t2 - t1, result);

            return result;
        }

        static int Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            List<string> tests = new List<string>();
            var scriptDirectory = Directory.GetFiles(Path.Combine($"{Environment.CurrentDirectory}", @"Scripts"));
            
            if (scriptDirectory.Length == 0)
            {
                throw new Exception("No test files found!");
            }

            foreach (var file in scriptDirectory)
            {
                tests.Add(file);
            }

            TestContext context = new TestContext(tests, 0);

            return context.Run();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ReleaseAssert.Fail("Unhandled exception: {0}", e.ExceptionObject);
        }
    }
}
