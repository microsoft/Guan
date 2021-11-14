///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.
//
// @File: PropertyMatchFunc.cs
//
// @Owner: xunlu
// @Test:  xunlu
//
// Purpose:
//   Function to match/compare trace property.
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
namespace Guan.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.RegularExpressions;

    public class PropertyMatchFunc : GuanFunc
    {
        public static readonly PropertyMatchFunc Equal = new PropertyMatchFunc("propequal", true, false);
        public static readonly PropertyMatchFunc OptionalEqual = new PropertyMatchFunc("propeq", true, false, true);
        public static readonly PropertyMatchFunc Match = new PropertyMatchFunc("propmatch", false, false);
        public static readonly PropertyMatchFunc NotEqual = new PropertyMatchFunc("propnotequal", true, true);
        public static readonly PropertyMatchFunc NotMatch = new PropertyMatchFunc("propnotmatch", false, true);

        private bool exactMatch;
        private bool negative;
        private bool optional;

        private PropertyMatchFunc(string name, bool exactMatch, bool negative, bool optional = false)
            : base(name)
        {
            this.exactMatch = exactMatch;
            this.negative = negative;
            this.optional = optional;
        }

        public override object Invoke(IPropertyContext context, object[] args)
        {
            int argCount = args.Length;
            if (argCount == 0)
            {
                throw new ArgumentException("Invalid number of arguments for Match()");
            }

            string propertyName = (string)args[0];
            string pattern = (argCount > 1 && args[1] != null ? args[1].ToString() : string.Empty);
            if (pattern == null && this.optional)
            {
                return true;
            }

            object propertyValue = GetFunc.Invoke(context, propertyName);
            string property = (propertyValue != null ? propertyValue.ToString() : string.Empty);

            bool result;
            if (this.exactMatch)
            {
                result = (property == pattern);
            }
            else
            {
                result = new Regex(pattern).IsMatch(property);
            }

            return (result != this.negative);
        }

        internal override GuanFunc Bind(List<GuanExpression> args)
        {
            string pattern;
            string propertyName;

            int argCount = args.Count;
            if (!this.exactMatch && args[argCount - 1].GetLiteral(out pattern))
            {
                if (!args[0].GetLiteral(out propertyName))
                {
                    return this;
                }

                args.Clear();

                return new BoundPropertyMatch(this.ToString(), propertyName, pattern, this.negative);
            }

            return this;
        }

        private class BoundPropertyMatch : GuanFunc
        {
            private string propertyName;
            private Regex pattern;
            private bool negative;

            public BoundPropertyMatch(string name, string propertyName, string pattern, bool negative)
                : base(name)
            {
                this.propertyName = propertyName;
                this.pattern = new Regex(pattern, RegexOptions.Compiled);
                this.negative = negative;
            }

            public override object Invoke(IPropertyContext context, object[] args)
            {
                object propertyValue = context[this.propertyName];
                string property = (propertyValue != null ? propertyValue.ToString() : string.Empty);

                return (this.pattern.IsMatch(property) != this.negative);
            }

            public override string ToString()
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}({1},{2})", this.Name, this.propertyName, this.pattern);
            }
        }
    }
}
