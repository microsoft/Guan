//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Globalization;
    using System.IO;
    using System.Text;

    public static class Utility
    {
        /// <summary>
        /// Retrieve a string configuration entry from application
        /// configuration file.
        /// </summary>
        /// <param name="name">Name of the entry.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The value of the entry, defaultValue if the
        /// entry is not found.</returns>
        public static string GetConfig(string name, string defaultValue = null)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (value == null)
            {
                return defaultValue;
            }

            return value;
        }

        /// <summary>
        /// Format the time into a string.
        /// </summary>
        /// <param name="time">The time stamp.</param>
        /// <returns>The string representation of the time stamp.</returns>
        public static string FormatTime(DateTime time)
        {
            if (time == DateTime.MinValue)
            {
                return "MIN";
            }

            if (time == DateTime.MaxValue)
            {
                return "MAX";
            }

            return time.ToString("yyyy-M-d HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        public static string FormatTimeWithTicks(DateTime time)
        {
            long ticks = time.Ticks % TimeSpan.TicksPerMillisecond;
            string result = FormatTime(time);
            if (ticks > 0 && time != DateTime.MaxValue && time != DateTime.MinValue)
            {
                result = result + "+" + ticks.ToString();
            }

            return result;
        }

        /// <summary>
        /// Round down a time stamp to milli-second.
        /// </summary>
        /// <param name="time">The original time stamp.</param>
        /// <returns>The time stamp after round down.</returns>
        public static DateTime TimeRounddown(DateTime time)
        {
            long ticks = time.Ticks;
            return new DateTime(ticks - (ticks % TimeSpan.TicksPerMillisecond));
        }

        /// <summary>
        /// Round up a time stamp to milli-second.
        /// </summary>
        /// <param name="time">The original time stamp.</param>
        /// <returns>The time stamp after round up.</returns>
        public static DateTime TimeRoundup(DateTime time)
        {
            long ticks = time.Ticks - 1;
            return new DateTime(ticks - (ticks % TimeSpan.TicksPerMillisecond) + TimeSpan.TicksPerMillisecond);
        }

        public static bool TryParse(string value, out bool result)
        {
            result = (!string.IsNullOrEmpty(value) && value != "false");
            return true;
        }

        public static bool TryParse(string value, out DateTime result)
        {
            if (string.Compare(value, "min", true, CultureInfo.InstalledUICulture) == 0)
            {
                result = DateTime.MinValue;
                return true;
            }

            if (string.Compare(value, "max", true, CultureInfo.InstalledUICulture) == 0)
            {
                result = DateTime.MaxValue;
                return true;
            }

            return DateTime.TryParse(value, out result);
        }

        public static bool TryParse(string value, Type type, out object result)
        {
            if (type == typeof(bool))
            {
                result = (!string.IsNullOrEmpty(value) && value != "false");
                return true;
            }

            System.ComponentModel.TypeConverter converter = System.ComponentModel.TypeDescriptor.GetConverter(type);
            if (converter == null)
            {
                result = null;
                return false;
            }

            result = converter.ConvertFromString(value);
            return true;
        }

        public static bool TryParse<T>(string value, out T result)
        {
            object obj;
            bool success = TryParse(value, typeof(T), out obj);
            result = (success ? (T)obj : default(T));
            return success;
        }

        public static bool TryParse<T>(string value, T defaultValue, out T result)
        {
            if (string.IsNullOrEmpty(value))
            {
                result = defaultValue;
                return true;
            }

            return Utility.TryParse<T>(value, out result);
        }

        public static bool TryConvert<T>(object value, out T result)
        {
            if (value == null)
            {
                result = default(T);
                return true;
            }

            if (typeof(T).IsAssignableFrom(value.GetType()))
            {
                result = (T)value;
                return true;
            }

            return Utility.TryParse(value.ToString(), out result);
        }

        public static T Convert<T>(object value)
        {
            T result;
            if (!TryConvert(value, out result))
            {
                ReleaseAssert.Fail("Can't convert {0} to {1}", value, typeof(T));
            }

            return result;
        }

        public static T MaxValue<T>()
        {
            if (typeof(T) == typeof(long))
            {
                return (T)(object)long.MaxValue;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)ulong.MaxValue;
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)double.MaxValue;
            }

            throw new NotSupportedException(typeof(T).ToString());
        }

        public static T MinValue<T>()
        {
            if (typeof(T) == typeof(long))
            {
                return (T)(object)long.MinValue;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)ulong.MinValue;
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)double.MinValue;
            }

            throw new NotSupportedException(typeof(T).ToString());
        }

        public static object CreateInstanceByReflection(string classSpec)
        {
            Type type = Type.GetType(classSpec);
            if (type == null)
            {
                throw new ArgumentException("Can't find type: " + classSpec);
            }

            return Activator.CreateInstance(type);
        }

        public static string CollectionToString<T>(List<T> list)
        {
            StringBuilder result = new StringBuilder();

            foreach (T item in list)
            {
                _ = result.AppendFormat("{0},", item);
            }

            if (result.Length > 0)
            {
                result.Length--;
            }

            return result.ToString();
        }

        public static string FormatString(string format, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }

        public static TimeSpan SafeSubtract(DateTime t1, DateTime t2)
        {
            if (t1 == DateTime.MaxValue)
            {
                ReleaseAssert.IsTrue(t2 != DateTime.MaxValue);
                return TimeSpan.MaxValue;
            }

            if (t2 == DateTime.MinValue)
            {
                ReleaseAssert.IsTrue(t1 != DateTime.MinValue);
                return TimeSpan.MaxValue;
            }

            return t1 - t2;
        }

        public static DateTime SafeSubtract(DateTime time, TimeSpan duration)
        {
            if (duration == TimeSpan.MaxValue)
            {
                return DateTime.MinValue;
            }

            if (time == DateTime.MaxValue || time == DateTime.MinValue)
            {
                return time;
            }

            return time - duration;
        }

        public static TimeSpan SafeSubtract(TimeSpan t1, TimeSpan t2)
        {
            if (t1 == TimeSpan.MaxValue || t2 == TimeSpan.MinValue)
            {
                return TimeSpan.MaxValue;
            }

            return t1 - t2;
        }

        public static DateTime SafeAdd(DateTime time, TimeSpan duration)
        {
            if (duration == TimeSpan.MaxValue)
            {
                return DateTime.MaxValue;
            }

            if (time == DateTime.MaxValue || time == DateTime.MinValue)
            {
                return time;
            }

            return time + duration;
        }

        public static DateTime Max(DateTime t1, DateTime t2)
        {
            return (t1 > t2 ? t1 : t2);
        }

        public static DateTime Min(DateTime t1, DateTime t2)
        {
            return (t1 < t2 ? t1 : t2);
        }

        public static TimeSpan Max(TimeSpan t1, TimeSpan t2)
        {
            return (t1 > t2 ? t1 : t2);
        }

        public static TimeSpan Min(TimeSpan t1, TimeSpan t2)
        {
            return (t1 < t2 ? t1 : t2);
        }

        public static TimeSpan AbsoluteValue(TimeSpan interval)
        {
            return (interval < TimeSpan.Zero ? -interval : interval);
        }

        public static bool TryCompare(object arg1, object arg2, out int result)
        {
            if (arg1 == null)
            {
                result = (arg2 == null ? 0 : -1);
                return true;
            }
            else if (arg2 == null)
            {
                result = 1;
                return true;
            }

            if (object.Equals(arg1, arg2))
            {
                result = 0;
                return true;
            }

            IComparable comparable = arg1 as IComparable;
            if (comparable != null)
            {
                //TODO: consider supporting args with different types (e.g. int vs. long)
                result = comparable.CompareTo(arg2);
                return true;
            }

            result = 0;
            return false;
        }
    }
}
