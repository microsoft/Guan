// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.RegularExpressions;

    /// <summary>
    /// The abstract function that must be inherited from for
    /// all property functions.
    /// A property function calculates a value based on arguments
    /// and a context, which exposes properties.
    /// </summary>
    public abstract class GuanFunc
    {
        private static object tableLock = new object();
        private static Dictionary<string, GuanFunc> funcTable = new Dictionary<string, GuanFunc>();
        private static bool externalResourceLoaded = false;

        private readonly string name;

        protected GuanFunc(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Function name.
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }
        }

        /// <summary>
        /// Set an external property function.
        /// This allows the user to supply their own functions.
        /// </summary>
        /// <param name="func">The function to be set.</param>
        public static void Add(GuanFunc func)
        {
            if (func == null)
            {
                throw new ArgumentNullException("func");
            }

            lock (tableLock)
            {
                Dictionary<string, GuanFunc> table = new Dictionary<string, GuanFunc>(funcTable);
                table[func.ToString()] = func;

                funcTable = table;
            }
        }

        /// <summary>
        /// Get a function object based on its name.
        /// </summary>
        /// <param name="name">Name of the function.</param>
        /// <returns>The function object.</returns>
        public static GuanFunc Get(string name, IGuanExpressionContext context)
        {
            GuanFunc result = (context != null ? context.GetFunc(name) : null);
            if (result == null)
            {
                if (!externalResourceLoaded)
                {
                    lock (tableLock)
                    {
                        if (!externalResourceLoaded)
                        {
                            AutoLoadResource.LoadResources(typeof(GuanFunc));
                            externalResourceLoaded = true;
                        }
                    }
                }

                _ = funcTable.TryGetValue(name, out result);
            }

            return result;
        }

        /// <summary>
        /// The main method to be overridden by implementing class.
        /// Result object should be calculated based on the context
        /// and the arguments.
        /// </summary>
        /// <param name="context">The context class.</param>
        /// <param name="args">The array of arguments to the function.</param>
        /// <returns>The function result.</returns>
        public abstract object Invoke(IPropertyContext context, object[] args);

        public override string ToString()
        {
            return this.name;
        }

        /// <summary>
        /// This allows the possibility to invoke a function
        /// without fully evaluate all of the arguments (e.g. And).
        /// Most implementing classes do not need to override this.
        /// </summary>
        /// <param name="context">The context class.</param>
        /// <param name="args">The collection of arguments to the function.</param>
        /// <returns>The function result.</returns>
        internal virtual object Invoke(IPropertyContext context, List<GuanExpression> args)
        {
            object[] evaluatedArgs;

            if (args != null)
            {
                evaluatedArgs = new object[args.Count];

                for (int i = 0; i < args.Count; i++)
                {
                    evaluatedArgs[i] = args[i].Evaluate(context);
                }
            }
            else
            {
                evaluatedArgs = null;
            }

            return this.Invoke(context, evaluatedArgs);
        }

        /// <summary>
        /// Optimize the function by binding some arguments to literal
        /// values.
        /// </summary>
        /// <param name="args">The arguments to the function.</param>
        /// <returns>The optimized function object.  If no binding
        /// optimization can be made, the function object itself
        /// should be returned.
        /// </returns>
        internal virtual GuanFunc Bind(List<GuanExpression> args)
        {
            return this;
        }

        /// <summary>
        /// Function for regular expression matching.
        /// The first parameter is string in question and the 2nd
        /// is the regular expression.
        /// </summary>
        internal class MatchFunc : BinaryFunc
        {
            public static readonly MatchFunc Singleton = new MatchFunc("match", false);
            public static readonly MatchFunc Not = new MatchFunc("notmatch", true);

            private readonly bool negative;

            private MatchFunc(string name, bool negative)
                : base(name)
            {
                this.negative = negative;
            }

            internal override GuanFunc Bind(List<GuanExpression> args)
            {
                if (args.Count == 2)
                {
                    string pattern;
                    if (args[1].GetLiteral(out pattern))
                    {
                        args.RemoveAt(1);

                        return new PatternFunc(pattern, this.negative);
                    }
                }

                return this;
            }

            protected override object InvokeBinary(object arg1, object arg2)
            {
                string s2 = arg2 as string;
                if (s2 == null)
                {
                    throw new ArgumentException("arg2 must be a regular expression");
                }

                string s1 = arg1 as string;
                if (s1 == null)
                {
                    if (arg1 == null)
                    {
                        return false;
                    }

                    s1 = arg1.ToString();
                }

                return (new Regex(s2).IsMatch(s1) != this.negative);
            }
        }

        internal class GetFromObjectFunc : StandaloneFunc
        {
            public static readonly GetFromObjectFunc Singleton = new GetFromObjectFunc("getobject");

            private GetFromObjectFunc(string name)
                : base(name)
            {
            }

            public override object Invoke(object[] args)
            {
                ReleaseAssert.IsTrue(args.Length == 2);
                Term term0 = args[0] as Term;
                return GetFunc.Invoke(term0 != null ? term0.GetObjectValue() : args[0], (string)args[1]);
            }
        }

        /// <summary>
        /// Function for regular expression matching with given regular expression.
        /// </summary>
        internal class PatternFunc : UnaryFunc
        {
            private readonly Regex pattern;
            private readonly bool negative;

            public PatternFunc(string pattern, bool negative)
                : base("pattern:" + pattern)
            {
                this.pattern = new Regex(pattern, RegexOptions.Compiled);
                this.negative = negative;
            }

            public override object UnaryInvoke(object arg)
            {
                string text = arg as string;
                if (text == null)
                {
                    if (arg == null)
                    {
                        return false;
                    }

                    text = arg.ToString();
                }

                return (this.pattern.IsMatch(text) != this.negative);
            }
        }

        internal class ToLowerFunc : UnaryFunc
        {
            public static readonly ToLowerFunc Singleton = new ToLowerFunc();

            protected ToLowerFunc()
                : base("ToLower")
            {
            }

            public override object UnaryInvoke(object arg)
            {
                if (arg == null)
                {
                    return string.Empty;
                }

                string result = arg.ToString();
                return result?.ToLower(CultureInfo.InvariantCulture);
            }
        }

        internal class ToUpperFunc : UnaryFunc
        {
            public static readonly ToUpperFunc Singleton = new ToUpperFunc();

            protected ToUpperFunc()
                : base("ToUpper")
            {
            }

            public override object UnaryInvoke(object arg)
            {
                if (arg == null)
                {
                    return string.Empty;
                }

                string result = arg.ToString();
                return result?.ToUpper(CultureInfo.InvariantCulture);
            }
        }

        internal class ExistsFunc : GuanFunc
        {
            public static readonly ExistsFunc Singleton = new ExistsFunc();

            private ExistsFunc()
                : base("Exists")
            {
            }

            public override object Invoke(IPropertyContext context, object[] args)
            {
                object result = GetFunc.Singleton.Invoke(context, args);
                return (result != null);
            }
        }

        internal class NotExistsFunc : GuanFunc
        {
            public static readonly NotExistsFunc Singleton = new NotExistsFunc();

            private NotExistsFunc()
                : base("NotExists")
            {
            }

            public override object Invoke(IPropertyContext context, object[] args)
            {
                object result = GetFunc.Singleton.Invoke(context, args);
                return (result == null);
            }
        }

        internal class NotEmptyFunc : GuanFunc
        {
            public static readonly NotEmptyFunc Singleton = new NotEmptyFunc();

            private NotEmptyFunc()
                : base("NotEmpty")
            {
            }

            public override object Invoke(IPropertyContext context, object[] args)
            {
                foreach (object arg in args)
                {
                    if (arg == null)
                    {
                        return false;
                    }

                    string stringArg = arg as string;
                    if (stringArg != null && stringArg.Length == 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        internal class EmptyFunc : GuanFunc
        {
            public static readonly EmptyFunc Singleton = new EmptyFunc();

            private EmptyFunc()
                : base("Empty")
            {
            }

            public override object Invoke(IPropertyContext context, object[] args)
            {
                foreach (object arg in args)
                {
                    if (arg == null)
                    {
                        return true;
                    }

                    string stringArg = arg as string;
                    if (string.IsNullOrEmpty(stringArg))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal class ExpressionFunc : GuanFunc
        {
            public static readonly ExpressionFunc Singleton = new ExpressionFunc();

            private ExpressionFunc()
                : base("expression")
            {
            }

            public override object Invoke(IPropertyContext context, object[] args)
            {
                return AddFunc.Singleton.Invoke(args);
            }
        }

        internal class DateTimeFunc : UnaryFunc
        {
            public static readonly DateTimeFunc Singleton = new DateTimeFunc();

            private DateTimeFunc()
                : base("DateTime")
            {
            }

            public override object UnaryInvoke(object arg)
            {
                return DateTime.Parse((string)arg);
            }
        }

        internal class TimeSpanFunc : UnaryFunc
        {
            public static readonly TimeSpanFunc Singleton = new TimeSpanFunc();

            private TimeSpanFunc()
                : base("TimeSpan")
            {
            }

            public override object UnaryInvoke(object arg)
            {
                return TimeSpan.Parse((string)arg);
            }
        }

        internal class IntFunc : UnaryFunc
        {
            public static readonly IntFunc Singleton = new IntFunc();

            private IntFunc()
                : base("int")
            {
            }

            public override object UnaryInvoke(object arg)
            {
                return Utility.Convert<int>(arg);
            }
        }

        internal class LongFunc : UnaryFunc
        {
            public static readonly LongFunc Singleton = new LongFunc();

            private LongFunc()
                : base("long")
            {
            }

            public override object UnaryInvoke(object arg)
            {
                return Utility.Convert<long>(arg);
            }
        }

        internal class DoubleFunc : UnaryFunc
        {
            public static readonly DoubleFunc Singleton = new DoubleFunc();

            private DoubleFunc()
                : base("double")
            {
            }

            public override object UnaryInvoke(object arg)
            {
                return Utility.Convert<double>(arg);
            }
        }

        internal class ListFunc : StandaloneFunc
        {
            public static readonly ListFunc Singleton = new ListFunc();

            private ListFunc()
                : base("List")
            {
            }

            public override object Invoke(object[] args)
            {
                List<object> result = new List<object>(args.Length);
                foreach (object arg in args)
                {
                    result.Add(arg);
                }

                return result;
            }
        }

        internal class MinMaxFunc : StandaloneFunc
        {
            public static readonly MinMaxFunc Min = new MinMaxFunc("min");
            public static readonly MinMaxFunc Max = new MinMaxFunc("max");

            private ComparisonFunc func;

            private MinMaxFunc(string name)
                : base(name)
            {
                this.func = (name == "min" ? ComparisonFunc.LT : ComparisonFunc.GT);
            }

            public override object Invoke(object[] args)
            {
                object result = args[0];

                for (int i = 1; i < args.Length; i++)
                {
                    if (this.func.Invoke(args[i], result))
                    {
                        result = args[i];
                    }
                }

                return result;
            }
        }

        internal class TimeFunc : StandaloneFunc
        {
            public static readonly TimeFunc Singleton = new TimeFunc();

            private TimeFunc()
                : base("time")
            {
            }

            public override object Invoke(object[] args)
            {
                if (args.Length > 1)
                {
                    throw new ArgumentException("Time() does not take more than one argument");
                }

                if (args.Length == 0)
                {
                    return DateTime.UtcNow;
                }

                return DateTime.UtcNow + (TimeSpan)args[0];
            }
        }

        internal class GuidFunc : StandaloneFunc
        {
            public static readonly GuidFunc Singleton = new GuidFunc();

            private GuidFunc()
                : base("guid")
            {
            }

            public override object Invoke(object[] args)
            {
                if (args.Length > 0)
                {
                    throw new ArgumentException("guid() does not take argument");
                }

                return Guid.NewGuid().ToString();
            }
        }
    }
}
