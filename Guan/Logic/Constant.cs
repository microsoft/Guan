// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Guan.Logic
{
    /// <summary>
    /// Constant term.
    /// </summary>
    public class Constant : Term
    {
        private object value_;

        /// <summary>
        /// Wrapping a null object.
        /// </summary>
        public static readonly Constant Null = new Constant(null);

        /// <summary>
        /// Empty list.
        /// </summary>
        public static readonly Constant Nil = new Constant(new object());

        public Constant(object value)
        {
            value_ = value;
        }

        public object Value
        {
            get
            {
                return value_;
            }
        }

        public override bool IsGround()
        {
            return true;
        }

        internal bool IsTrue()
        {
            if (value_ == null)
            {
                return false;
            }

            if (value_ is bool)
            {
                return (bool)value_;
            }

            string s = value_ as string;
            if (s != null)
            {
                return s.Length > 0;
            }

            return true;
        }

        public override string ToString()
        {
            if (this == Nil)
            {
                return "[]";
            }

            return (value_ != null ? value_.ToString() : "");
        }

        public static Constant Parse(string text)
        {
            if (text.StartsWith("'") && text.EndsWith("'") && text.Length > 1)
            {
                return new Constant(text.Substring(1, text.Length - 2));
            }

            if (text == "null")
            {
                return Null;
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

            if (bool.TryParse(text, out bool boolValue))
            {
                return new Constant(boolValue);
            }

            return new Constant(text);
        }
    }
}
