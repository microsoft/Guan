// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;

namespace Guan.Common
{
    /// <summary>
    /// The abstract function that must be inherited from for
    /// all property functions.
    /// A property function calculates a value based on arguments
    /// and a context, which exposes properties.
    /// </summary>
    public abstract class GuanFunc
    {
        private string m_name;

        private static object s_tableLock = new object();
        private static Dictionary<string, GuanFunc> s_table = new Dictionary<string, GuanFunc>();
        private static bool ExternalResourceLoaded = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        protected GuanFunc(string name)
        {
            m_name = name;
        }

        /// <summary>
        /// Function name.
        /// </summary>
        public string Name
        {
            get
            {
                return m_name;
            }
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

            return Invoke(context, evaluatedArgs);
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
        /// Name of the function.
        /// </summary>
        /// <returns>Name of the function.</returns>
        public override string ToString()
        {
            return m_name;
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

            lock (s_tableLock)
            {
                Dictionary<string, GuanFunc> table = new Dictionary<string, GuanFunc>(s_table)
                {
                    [func.ToString()] = func
                };

                s_table = table;
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
                if (!ExternalResourceLoaded)
                {
                    lock (s_tableLock)
                    {
                        if (!ExternalResourceLoaded)
                        {
                            AutoLoadResource.LoadResources(typeof(GuanFunc));
                            ExternalResourceLoaded = true;
                        }
                    }
                }

                s_table.TryGetValue(name, out result);
            }

            return result;
        }
    }

    /// <summary>
    /// Functions that do not depend on the context.
    /// </summary>
    public abstract class StandaloneFunc : GuanFunc
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the function.</param>
        protected StandaloneFunc(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Invoke the function.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>Function result.</returns>
        public abstract object Invoke(object[] args);

        /// <summary>
        /// Invoke the function.
        /// </summary>
        /// <param name="context">The context class.</param>
        /// <param name="args">The array of arguments to the function.</param>
        /// <returns>The function result.</returns>
        public sealed override object Invoke(IPropertyContext context, object[] args)
        {
            return Invoke(args);
        }

        /// <summary>
        /// Replace the function with the only child argument.
        /// This is used during Bind to simplify the operation
        /// tree if the function result is the same as its only
        /// argument.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The optimized function object.</returns>
        internal GuanFunc Collapse(IList<GuanExpression> args)
        {
            if (args.Count == 1)
            {
                GuanExpression arg = args[0];

                // Replace arguments with the ones of the argument.
                args.Clear();
                foreach (GuanExpression child in arg.Children)
                {
                    args.Add(child);
                }

                return arg.Func;
            }

            return this;
        }
    }

    /// <summary>
    /// Literal "function" which always returns the literal value itself.
    /// </summary>
    public class Literal : StandaloneFunc
    {
        private object m_value;

        internal static readonly Literal Empty = new Literal(null);
        internal static readonly Literal True = new Literal(true);
        internal static readonly Literal False = new Literal(false);

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value">The value of the literal.</param>
        public Literal(object value)
            : base(value != null ? value.ToString() : string.Empty)
        {
            m_value = value;
        }

        public override object Invoke(object[] args)
        {
            return m_value;
        }
    }

    /// <summary>
    /// Functions that take only one argument.
    /// </summary>
    public abstract class UnaryFunc : StandaloneFunc
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the function.</param>
        protected UnaryFunc(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Invoke the function with one argument.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <returns>Function result.</returns>
        public abstract object UnaryInvoke(object arg);

        /// <summary>
        /// Invoke the function.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>Function result.</returns>
        public sealed override object Invoke(object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            if (args.Length != 1)
            {
                throw new ArgumentException("Invalid argument for UnaryFunc: " + this);
            }

            return UnaryInvoke(args[0]);
        }
    }

    /// <summary>
    /// Functions that take two arguments.
    /// </summary>
    public abstract class BinaryFunc : StandaloneFunc
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the function.</param>
        protected BinaryFunc(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Invoke the function with two arguments.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <returns>Function result.</returns>
        protected abstract object InvokeBinary(object arg1, object arg2);

        /// <summary>
        /// Invoke the function.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>Function result.</returns>
        public override object Invoke(object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            if (args.Length != 2)
            {
                throw new ArgumentException("Invalid argument for BinaryFunc: " + this);
            }

            return InvokeBinary(args[0], args[1]);
        }
    }

    /// <summary>
    /// Function for parsing a string into an object.
    /// </summary>
    public abstract class ObjectParserFunc : UnaryFunc
    {
        protected ObjectParserFunc(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Parse string into an object.
        /// </summary>
        /// <param name="data">String representation of an object.</param>
        /// <returns>The object parsed.</returns>
        protected abstract object Parse(string data);

        public override object UnaryInvoke(object arg)
        {
            string data = (string) arg;
            if (!string.IsNullOrEmpty(data))
            {
                data = data.Trim();
            }

            if (string.IsNullOrEmpty(data))
            {
                return null;
            }

            return Parse(data);
        }
    }

    /// <summary>
    /// And function.
    /// </summary>
    public class AndFunc : StandaloneFunc
    {
        public static readonly AndFunc Singleton = new AndFunc();

        private AndFunc()
            : base("and")
        {
        }

        public override object Invoke(object[] args)
        {
            foreach (object arg in args)
            {
                if (!(bool) arg)
                {
                    return false;
                }
            }

            return true;
        }

        internal override object Invoke(IPropertyContext context, List<GuanExpression> args)
        {
            foreach (GuanExpression exp in args)
            {
                object arg = exp.Evaluate(context);
                if ((arg == null) || !((bool) arg))
                {
                    return false;
                }
            }

            return true;
        }

        internal override GuanFunc Bind(List<GuanExpression> args)
        {
            for (int i = args.Count - 1; i >= 0; i--)
            {
                bool arg;
                if (args[i].GetLiteral(out arg))
                {
                    if (!arg)
                    {
                        return Literal.False;
                    }

                    args.RemoveAt(i);
                }
            }

            return Collapse(args);
        }
    }

    /// <summary>
    /// Or function.
    /// </summary>
    public class OrFunc : StandaloneFunc
    {
        public static readonly OrFunc Singleton = new OrFunc();

        private OrFunc()
            : base("or")
        {
        }

        public override object Invoke(object[] args)
        {
            foreach (object arg in args)
            {
                if ((arg != null) && (bool) arg)
                {
                    return true;
                }
            }

            return false;
        }

        internal override object Invoke(IPropertyContext context, List<GuanExpression> args)
        {
            foreach (GuanExpression exp in args)
            {
                bool arg = (bool) exp.Evaluate(context); 
                if (arg)
                {
                    return true;
                }
            }

            return false;
        }

        internal override GuanFunc Bind(List<GuanExpression> args)
        {
            for (int i = args.Count - 1; i >= 0; i--)
            {
                bool arg;
                if (args[i].GetLiteral(out arg))
                {
                    if (arg)
                    {
                        return Literal.True;
                    }

                    args.RemoveAt(i);
                }
            }

            return Collapse(args);
        }
    }

    /// <summary>
    /// Not function.
    /// </summary>
    public class NotFunc : UnaryFunc
    {
        public static readonly NotFunc Singleton = new NotFunc();

        private NotFunc()
            : base("not")
        {
        }

        public override object UnaryInvoke(object arg)
        {
            return (arg == null || !(bool) arg);
        }
    }

    /// <summary>
    /// Comparison functions.
    /// If any of the parameter is a number (convertable to long),
    /// the comparison will be performed after converting both to long
    /// Otherwise string comparisons will be used.
    /// </summary>
    public class ComparisonFunc : BinaryFunc
    {
        [Flags]
        enum Option
        {
            LessThan = 1,
            Equal = 2,
            GreaterThan = 4
        }

        private readonly Option m_option;

        public static readonly ComparisonFunc EQ = new ComparisonFunc("eq", Option.Equal);
        public static readonly ComparisonFunc NE = new ComparisonFunc("ne", Option.GreaterThan | Option.LessThan);
        public static readonly ComparisonFunc GT = new ComparisonFunc("gt", Option.GreaterThan);
        public static readonly ComparisonFunc LT = new ComparisonFunc("lt", Option.LessThan);
        public static readonly ComparisonFunc GE = new ComparisonFunc("ge", Option.GreaterThan | Option.Equal);
        public static readonly ComparisonFunc LE = new ComparisonFunc("le", Option.LessThan | Option.Equal);

        private ComparisonFunc(string name, Option option)
            : base(name)
        {
            m_option = option;
        }

        public bool Invoke(object arg1, object arg2)
        {
            int result;
            ReleaseAssert.IsTrue(Utility.TryCompare(arg1, arg2, out result));
            return Compare(result);
        }

        protected override object InvokeBinary(object arg1, object arg2)
        {
            int result;
            ReleaseAssert.IsTrue(Utility.TryCompare(arg1, arg2, out result),
                "Incompatible arguments to compare {0}/{1}:{2}/{3}",
                arg1, arg1 != null ? arg1.GetType() : null, arg2, arg2 != null ? arg2.GetType() : null);
            return Compare(result);
        }

        internal bool Compare(int cmp)
        {
            if (cmp == 0)
            {
                return ((m_option & Option.Equal) != 0);
            }

            if (cmp < 0)
            {
                return ((m_option & Option.LessThan) != 0);
            }

            return ((m_option & Option.GreaterThan) != 0);
        }

        public ComparisonFunc Inverse()
        {
            if (this == ComparisonFunc.LT)
            {
                return ComparisonFunc.GT;
            }
            else if (this == ComparisonFunc.GT)
            {
                return ComparisonFunc.LT;
            }
            else if (this == ComparisonFunc.LE)
            {
                return ComparisonFunc.GE;
            }
            else if (this == ComparisonFunc.GE)
            {
                return ComparisonFunc.LE;
            }

            return this;
        }
    }

    /// <summary>
    /// Function for regular expression matching.
    /// The first parameter is string in question and the 2nd
    /// is the regular expression.
    /// </summary>
    public class MatchFunc : BinaryFunc
    {
        private bool m_negation;

        public static readonly MatchFunc Singleton = new MatchFunc("match", false);
        public static readonly MatchFunc Not = new MatchFunc("notmatch", true);

        private MatchFunc(string name, bool negation)
            : base(name)
        {
            m_negation = negation;
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

            return (new Regex(s2).IsMatch(s1) != m_negation);
        }

        internal override GuanFunc Bind(List<GuanExpression> args)
        {
            if (args.Count == 2)
            {
                string pattern;
                if (args[1].GetLiteral(out pattern))
                {
                    args.RemoveAt(1);

                    return new PatternFunc(pattern, m_negation);
                }
            }

            return this;
        }
    }

    /// <summary>
    /// Function for regular expression matching with given regular expression.
    /// </summary>
    internal class PatternFunc : UnaryFunc
    {
        private Regex m_pattern;
        private bool m_negation;

        public PatternFunc(string pattern, bool negation)
            : base("pattern:" + pattern)
        {
            m_pattern = new Regex(pattern, RegexOptions.Compiled);
            m_negation = negation;
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

            return (m_pattern.IsMatch(text) != m_negation);
        }
    }

    /// <summary>
    /// Function to retrieve property from context.
    /// </summary>
    public class GetFunc : GuanFunc
    {
        public static readonly GetFunc Singleton = new GetFunc("get");

        private static readonly char[] s_delimiters = new char[] { ',', ' ' };
        private static readonly Type[] s_stringType = new Type[] { typeof(string) };

        private GetFunc(string name)
            : base(name)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Type.InvokeMember", Justification="Property name specified by user at runtime.")]
        public override object Invoke(IPropertyContext context, object[] args)
        {
            ReleaseAssert.IsTrue(args.Length == 1 && context != null);
            return Invoke(context, (string)args[0]);
        }

        internal static object Invoke(object context, string name)
        {
            object result = context;
            while (result != null && !string.IsNullOrEmpty(name))
            {
                // Get the next level property name.
                string propertyName;

                int level = 0;
                int index;

                for (index = 0; index < name.Length; index++)
                {
                    if (name[index] == '[')
                    {
                        level++;
                    }
                    else if (name[index] == ']')
                    {
                        level--;
                        // Can't be well-formed [] any more.
                        if (level < 0)
                        {
                            level = int.MinValue;
                        }
                    }
                    else if (name[index] == '.' && level == 0)
                    {
                        break;
                    }
                }

                if (index < name.Length)
                {
                    propertyName = name.Substring(0, index);
                    name = name.Substring(index + 1);
                }
                else
                {
                    propertyName = name;
                    name = null;
                }

                IPropertyContext propertyContext = result as IPropertyContext;
                if (propertyContext != null)
                {
                    result = propertyContext[propertyName];
                }
                else
                {
                    // Use reflection if no other option.
                    Type type = result.GetType();

                    // Allow access to methods with only string parameters.
                    // Use [] instead of () to enclose the parameters because
                    // otherwise it can be interpreted as a GuanFunc.
                    if (propertyName.EndsWith("]", StringComparison.Ordinal))
                    {
                        index = propertyName.LastIndexOf('[');
                        if (index < 0)
                        {
                            throw new ArgumentException("Invalid property name");
                        }

                        string method = propertyName.Substring(0, index);
                        string param = propertyName.Substring(index + 1, propertyName.Length - index - 2);

                        result = InvokeMethod(type, method, param, result);
                    }
                    else
                    {
                        result = type.InvokeMember(propertyName, BindingFlags.GetProperty, null, result, null, CultureInfo.InvariantCulture);
                    }
                }
            }

            return result;
        }

        private static object InvokeMethod(Type type, string methodName, string parameters, object obj)
        {
            string[] args = parameters.Split(s_delimiters, StringSplitOptions.RemoveEmptyEntries);

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] paramInfo = method.GetParameters();
                if (paramInfo.Length == args.Length)
                {
                    object[] paramObjects = new object[args.Length];

                    int i;
                    for (i = 0; i < args.Length; i++)
                    {
                        if (!Convert(paramInfo[i].ParameterType, args[i], out paramObjects[i]))
                        {
                            break;
                        }
                    }

                    if (i == args.Length)
                    {
                        return type.InvokeMember(methodName,
                                                 BindingFlags.InvokeMethod,
                                                 null, obj, paramObjects,
                                                 CultureInfo.InvariantCulture);
                    }
                }
            }

            throw new ArgumentException("Unable to find matching method:" + methodName);
        }

        private static bool Convert(Type type, string value, out object result)
        {
            if (type == typeof(string))
            {
                result = value;
                return true;
            }

            try
            {
                MethodInfo info = type.GetMethod("Parse",
                                                 BindingFlags.Static | BindingFlags.Public,
                                                 null, s_stringType, null);
                if (info != null)
                {
                    result = info.Invoke(null, new object[] { value });
                    return true;
                }

                ConstructorInfo constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance,
                                                                  null, s_stringType, null);
                if (constructor != null)
                {
                    result = constructor.Invoke(new object[] { value });
                    return true;
                }
            }
            catch (TargetInvocationException)
            {
            }

            result = null;

            return false;
        }
    }

    public class GetFromObjectFunc : StandaloneFunc
    {
        public static readonly GetFromObjectFunc Singleton = new GetFromObjectFunc("getobject");

        private GetFromObjectFunc(string name)
            : base(name)
        {
        }

        public override object Invoke(object[] args)
        {
            ReleaseAssert.IsTrue(args.Length == 2);
            return GetFunc.Invoke(args[0], (string)args[1]);
        }
    }

    internal class ToLowerFunc : UnaryFunc
    {
        public static ToLowerFunc Singleton = new ToLowerFunc();

        protected ToLowerFunc() : base("ToLower")
        {
        }

        public override object UnaryInvoke(object arg)
        {
            if (arg == null)
            {
                return string.Empty;
            }

            return arg.ToString().ToLower();
        }
    }

    internal class ToUpperFunc : UnaryFunc
    {
        public static ToUpperFunc Singleton = new ToUpperFunc();

        protected ToUpperFunc() : base("ToUpper")
        {
        }

        public override object UnaryInvoke(object arg)
        {
            if (arg == null)
            {
                return string.Empty;
            }

            return arg.ToString().ToLower();
        }
    }

    internal class ExistsFunc : GuanFunc
    {
        public static ExistsFunc Singleton = new ExistsFunc();

        private ExistsFunc() : base("Exists")
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
        public static NotExistsFunc Singleton = new NotExistsFunc();

        private NotExistsFunc() : base("NotExists")
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
        public static NotEmptyFunc Singleton = new NotEmptyFunc();

        private NotEmptyFunc() : base("NotEmpty")
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
        public static EmptyFunc Singleton = new EmptyFunc();

        private EmptyFunc() : base("Empty")
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
                if (stringArg.Length == 0)
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal class ExpressionFunc : GuanFunc
    {
        public static ExpressionFunc Singleton = new ExpressionFunc();

        private ExpressionFunc() : base("expression")
        {
        }

        public override object Invoke(IPropertyContext context, object[] args)
        {
            return AddFunc.Singleton.Invoke(args);
        }
    }

    internal class TimeFunc : UnaryFunc
    {
        public static TimeFunc Singleton = new TimeFunc();

        private TimeFunc() : base("time")
        {
        }

        public override object UnaryInvoke(object arg)
        {
            return DateTime.Parse((string)arg);
        }
    }

    internal class GuanTimeFunc : UnaryFunc
    {
        public static GuanTimeFunc Singleton = new GuanTimeFunc();

        private GuanTimeFunc() : base("GuanTime")
        {
        }

        public override object UnaryInvoke(object arg)
        {
            return new GuanTime(DateTime.Parse((string)arg));
        }
    }

    internal class TimeSpanFunc : UnaryFunc
    {
        public static TimeSpanFunc Singleton = new TimeSpanFunc();

        private TimeSpanFunc() : base("TimeSpan")
        {
        }

        public override object UnaryInvoke(object arg)
        {
            return TimeSpan.Parse((string)arg);
        }
    }

    internal class IntFunc : UnaryFunc
    {
        public static IntFunc Singleton = new IntFunc();

        private IntFunc() : base("int")
        {
        }

        public override object UnaryInvoke(object arg)
        {
            return Utility.Convert<int>(arg);
        }
    }

    internal class LongFunc : UnaryFunc
    {
        public static LongFunc Singleton = new LongFunc();

        private LongFunc() : base("long")
        {
        }

        public override object UnaryInvoke(object arg)
        {
            return Utility.Convert<long>(arg);
        }
    }

    internal class DoubleFunc : UnaryFunc
    {
        public static DoubleFunc Singleton = new DoubleFunc();

        private DoubleFunc() : base("double")
        {
        }

        public override object UnaryInvoke(object arg)
        {
            return Utility.Convert<double>(arg);
        }
    }

    internal class ListFunc : StandaloneFunc
    {
        public static ListFunc Singleton = new ListFunc();

        private ListFunc() : base("List")
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
        public static MinMaxFunc Min = new MinMaxFunc("min");
        public static MinMaxFunc Max = new MinMaxFunc("max");

        private ComparisonFunc func_;

        private MinMaxFunc(string name) : base(name)
        {
            func_ = (name == "min" ? ComparisonFunc.LT : ComparisonFunc.GT);
        }

        public override object Invoke(object[] args)
        {
            object result = args[0];

            for (int i = 1; i < args.Length; i++)
            {
                if (func_.Invoke(args[i], result))
                {
                    result = args[i];
                }
            }

            return result;
        }
    }
}
