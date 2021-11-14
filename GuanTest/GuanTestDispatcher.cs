using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Guan.Logic;

namespace GuanTest
{
    internal class GuanTestDispatcher : TestDispatcher
    {
        private List<string> rules_;
        private bool rulesReady_;
        private Module module_;

        public GuanTestDispatcher()
        {
            rules_ = new List<string>();
            rulesReady_ = true;
        }

        private static string GetTestFilePath(string location, string source)
        {
            if (!string.IsNullOrEmpty(location))
            {
                string path = Path.Combine(location, source);

                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private bool Expect(List<string> args)
        {
            for (int i = 1; i < args.Count; i++)
            {
                Verifier.Expect(args[i]);
            }

            return true;
        }

        private bool Verify(List<string> args)
        {
            int timeout = 0;
            if (args.Count > 1)
            {
                Utility.TryParse<int>(args[1], 0, out timeout);
            }

            Verifier.Wait(TimeSpan.FromSeconds(timeout));
            Report("Verify completed successfully");

            return true;
        }

        private static GuanObject ParseDictionary(List<string> args, int offset)
        {
            GuanObject result = new GuanObject();

            for (int i = offset; i < args.Count; i++)
            {
                string pair = args[i];
                int index = pair.IndexOf('=');
                if (index > 0)
                {
                    string key = pair.Substring(0, index).Trim();
                    string value = pair.Substring(index + 1).Trim();
                    result[key] = ParseArg(value);
                }
            }

            return result;
        }

        private static object ParseArg(string text)
        {
            int index = text.IndexOf('(');
            if (index > 0 && text.EndsWith(")"))
            {
                string value = text.Substring(index + 1, text.Length - index - 2);
                string name = text.Substring(0, index);
                if (name == "int")
                {
                    return int.Parse(value);
                }
                else if (name == "TimeSpan")
                {
                    return TimeSpan.Parse(value);
                }
            }

            return text;
        }

        private bool Enumerate(List<string> args)
        {
            if (args.Count < 4)
            {
                return false;
            }

            DateTime time1, time2;
            if (!Utility.TryParse(args[2], out time1) || !Utility.TryParse(args[3], out time2))
            {
                return false;
            }

            GuanPredicate filter = null;
            string option = string.Empty;
            int maxCount = int.MaxValue;
            int offset;
            for (offset = 4; offset < args.Count; offset++)
            {
                if (args[offset].StartsWith("filter:"))
                {
                    filter = GuanPredicate.Build(args[offset].Substring(7));
                }
                else if (args[offset].StartsWith("option:"))
                {
                    option = args[offset].Substring(7);
                }
                else if (args.Count > offset && args[offset].StartsWith("max:"))
                {
                    maxCount = int.Parse(args[offset].Substring(4));
                }
                else
                {
                    break;
                }
            }

            return true;
        }

        private bool Convert(List<string> args)
        {
            if (args.Count < 4)
            {
                return false;
            }

            return true;
        }

        private bool SetRules(List<string> args)
        {
            if (args.Count < 2)
            {
                return false;
            }

            rulesReady_ = false;

            if (args[1] == "clear")
            {
                int index = 0;
                if (args.Count > 2)
                {
                    index = int.Parse(args[2]);
                }

                rules_.RemoveRange(index, rules_.Count - index);
                return true;
            }

            for (int i = 1; i < args.Count; i++)
            {
                rules_.Add(args[i]);
            }

            return true;
        }

        private async Task<bool> TestQuery(List<string> args)
        {
            if (args.Count < 2)
            {
                return false;
            }

            int maxCount = int.MaxValue;
            DateTime startTime = DateTime.MinValue;
            DateTime endTime = DateTime.MaxValue;
            bool batch = false;
            
            for (int offset = 2; offset < args.Count; offset++)
            {
                if (args[offset].StartsWith("max:"))
                {
                    maxCount = int.Parse(args[offset].Substring(4));
                }
                else if (args[offset] == "batch")
                {
                    batch = true;
                }
                else
                {
                    if (Utility.TryParse(args[offset], out DateTime time))
                    {
                        if (startTime == DateTime.MinValue)
                        {
                            startTime = time;
                        }
                        else
                        {
                            endTime = time;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            QueryContext queryContext = new QueryContext();

            if (!rulesReady_ || module_ == null)
            {
                module_ = Module.Parse("test", rules_, null);
            }

            ModuleProvider moduleProvider = new ModuleProvider();
            moduleProvider.Add(module_);

            Query query = Query.Create(args[1], queryContext, moduleProvider);

            if (batch)
            {
                List<Term> results = query.GetResultsAsync(maxCount).Result;
                foreach (Term result in results)
                {
                    Report(TestSession.ResultReport, "{0}", result);
                }
            }
            else
            {
                int count = 0;
                while (count < maxCount)
                {
                    Term term = await query.GetNextAsync();
                    if (term == null)
                    {
                        break;
                    }

                    Report(TestSession.ResultReport, "{0}", term);
                    count++;
                }
            }

            return true;
        }

        public override bool ExecuteCommand(string command)
        {
            List<string> args = ParseCommand(command);

            switch (args[0])
            {
                case "resolve":
                case "enumerate":
                    return Enumerate(args);
                case "expect":
                    return Expect(args);
                case "verify":
                    return Verify(args);
                case "convert":
                    return Convert(args);
                case "rules":
                    return SetRules(args);
                case "query":
                    return TestQuery(args).Result;
                default:
                    return false;
            }
        }
    }
}
