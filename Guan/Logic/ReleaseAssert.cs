///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.
//
// @File: ReleaseAssert.cs
//
// @Owner: xunlu
// @Test:  xunlu
//
// Purpose:
//   Helper class for providing assert behavior in production environment.
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
namespace Guan.Logic
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Assertion class that will dump debug information and terminate the
    /// process when assert fails.
    /// Should be used to verify invariances.
    /// </summary>
    public static class ReleaseAssert
    {
        /// <summary>
        /// Terminate the process after logging debug information.
        /// </summary>
        /// <param name="format">The format string for logging.</param>
        /// <param name="args">The arguments for logging.  If the argument
        /// object supports IDumpable, this interface will be used to dump
        /// the object.  Otherwise ToString will be used.</param>
        public static void Fail(string format, params object[] args)
        {
            string message;
            if ((args != null) && (args.Length > 0))
            {
                message = string.Format(CultureInfo.InvariantCulture, format, args);
            }
            else
            {
                message = format;
            }

            ConsoleSink.WriteLine(ConsoleColor.Red, "Assert Failed: {0}\nStack:{1}", message, Environment.StackTrace);

            throw new SystemException("Assert Failed");
        }

        /// <summary>
        /// Assert that the condition is true.  Otherwise terminate the process
        /// after logging debug information.
        /// </summary>
        /// <param name="condition">The condition to assert.</param>
        /// <param name="format">The format string for logging.</param>
        /// <param name="args">The arguments for logging.  If the argument
        /// object supports IDumpable, this interface will be used to dump
        /// the object.  Otherwise ToString will be used.</param>
        public static void IsTrue(bool condition, string format, object[] args)
        {
            if (!condition)
            {
                Fail(format, args);
            }

            return;
        }

        /// <summary>
        /// Assert that the condition is true.  Otherwise terminate the process
        /// after logging debug information.
        /// </summary>
        /// <param name="condition">The condition to assert.</param>
        /// <param name="format">The format string for logging.</param>
        public static void IsTrue(bool condition, string format)
        {
            if (!condition)
            {
                Fail(format);
            }

            return;
        }

        /// <summary>
        /// Assert that the condition is true.  Otherwise terminate the process
        /// after logging debug information.
        /// </summary>
        /// <param name="condition">The condition to assert.</param>
        /// <param name="format">The format string for logging.</param>
        /// <param name="t1">The argument for logging.</param>
        public static void IsTrue<T1>(bool condition, string format, T1 t1)
        {
            if (!condition)
            {
                Fail(format, t1);
            }

            return;
        }

        public static void IsTrue<T1, T2>(bool condition, string format, T1 t1, T2 t2)
        {
            if (!condition)
            {
                Fail(format, t1, t2);
            }

            return;
        }

        public static void IsTrue<T1, T2, T3>(bool condition, string format, T1 t1, T2 t2, T3 t3)
        {
            if (!condition)
            {
                Fail(format, t1, t2, t3);
            }

            return;
        }

        public static void IsTrue<T1, T2, T3, T4>(bool condition, string format, T1 t1, T2 t2, T3 t3, T4 t4)
        {
            if (!condition)
            {
                Fail(format, t1, t2, t3, t4);
            }

            return;
        }

        public static void IsTrue<T1, T2, T3, T4, T5>(bool condition, string format, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        {
            if (!condition)
            {
                Fail(format, t1, t2, t3, t4, t5);
            }

            return;
        }

        public static void IsTrue<T1, T2, T3, T4, T5, T6>(bool condition, string format, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
        {
            if (!condition)
            {
                Fail(format, t1, t2, t3, t4, t5, t6);
            }

            return;
        }

        public static void IsTrue<T1, T2, T3, T4, T5, T6, T7>(bool condition, string format, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
        {
            if (!condition)
            {
                Fail(format, t1, t2, t3, t4, t5, t6, t7);
            }

            return;
        }

        public static void IsTrue<T1, T2, T3, T4, T5, T6, T7, T8>(bool condition, string format, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
        {
            if (!condition)
            {
                Fail(format, t1, t2, t3, t4, t5, t6, t7, t8);
            }

            return;
        }

        /// <summary>
        /// Assert that the condition is true.  Otherwise terminate the process
        /// after logging debug information.
        /// </summary>
        /// <param name="condition">The condition to assert.</param>
        public static void IsTrue(bool condition)
        {
            if (!condition)
            {
                Fail(string.Empty, null);
            }

            return;
        }
    }
}
