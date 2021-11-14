///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.
//
// @File: GetFunc.cs
//
// @Owner: xunlu
// @Test:  xunlu
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
namespace Guan.Logic
{
    using System;
    using System.Globalization;
    using System.Reflection;

    /// <summary>
    /// Function to retrieve property from context.
    /// </summary>
    internal class GetFunc : GuanFunc
    {
        public static readonly GetFunc Singleton = new GetFunc("get");

        private static readonly char[] Delimiters = new char[] { ',', ' ' };
        private static readonly Type[] StringType = new Type[] { typeof(string) };

        private GetFunc(string name)
            : base(name)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Type.InvokeMember", Justification = "Property name specified by user at runtime.")]
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
            string[] args = parameters.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);

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
                        return type.InvokeMember(
                            methodName,
                            BindingFlags.InvokeMethod,
                            null,
                            obj,
                            paramObjects,
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
                MethodInfo info = type.GetMethod(
                    "Parse",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    StringType,
                    null);
                if (info != null)
                {
                    result = info.Invoke(null, new object[] { value });
                    return true;
                }

                ConstructorInfo constructor = type.GetConstructor(
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    StringType,
                    null);
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
}
