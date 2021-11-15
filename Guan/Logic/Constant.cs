// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;

    /// <summary>
    /// Constant term.
    /// </summary>
    public class Constant : Term
    {
        /// <summary>
        /// Wrapping a null object.
        /// </summary>
        public static readonly Constant Null = new Constant(null);

        /// <summary>
        /// Empty list.
        /// </summary>
        public static readonly Constant Nil = new Constant(new EmptyList());

        /// <summary>
        /// Boolean True.
        /// </summary>
        public static readonly Constant True = new Constant(true);

        private object value;

        public Constant(object value)
        {
            this.value = value;
        }

        public object Value
        {
            get
            {
                return this.value;
            }
        }

        public static Constant Parse(string text)
        {
            if (text.Length > 1 && text[0] == text[text.Length - 1] && (text[0] == '\'' || text[0] == '"'))
            {
                return new Constant(text.Substring(1, text.Length - 2));
            }

            if (text == "null")
            {
                return Null;
            }

            if (bool.TryParse(text, out bool boolValue))
            {
                return new Constant(boolValue);
            }

            if (long.TryParse(text, out long longValue))
            {
                return new Constant(longValue);
            }

            if (ulong.TryParse(text, out ulong ulongValue))
            {
                return new Constant(ulongValue);
            }

            if (double.TryParse(text, out double doubleValue))
            {
                return new Constant(doubleValue);
            }

            if (TimeSpan.TryParse(text, out TimeSpan timeSpan))
            {
                return new Constant(timeSpan);
            }

            return new Constant(text);
        }

        public override bool IsGround()
        {
            return true;
        }

        public override string ToString()
        {
            if (this == Nil)
            {
                return "[]";
            }

            if (this.value?.GetType() == typeof(DateTime))
            {
                return Utility.FormatTime((DateTime)this.value);
            }

            if (this.value == null)
            {
                return string.Empty;
            }

            string stringValue = this.value as string;
            if (stringValue != null)
            {
                return "'" + stringValue + "'";
            }

            return this.value.ToString();
        }

        internal bool IsTrue()
        {
            if (this.value == null)
            {
                return false;
            }

            if (this.value is bool)
            {
                return (bool)this.value;
            }

            string s = this.value as string;
            if (s != null)
            {
                return s.Length > 0;
            }

            return true;
        }

        private class EmptyList
        {
        }
    }
}
