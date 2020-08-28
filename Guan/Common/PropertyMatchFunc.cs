// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Guan.Common
{
    public class PropertyMatchFunc : GuanFunc
    {
        class BoundPropertyMatch : GuanFunc
        {
            private string m_propertyName;
            private Regex m_pattern;
            private bool m_not;

            public BoundPropertyMatch(string name, string propertyName, string pattern, bool not)
                : base(name)
            {
                m_propertyName = propertyName;
                m_pattern = new Regex(pattern, RegexOptions.Compiled);
                m_not = not;
            }

            public override object Invoke(IPropertyContext context, object[] args)
            {
                object propertyValue = context[m_propertyName];
                string property = (propertyValue != null ? propertyValue.ToString() : string.Empty);

                return (m_pattern.IsMatch(property) != m_not);
            }

            public override string ToString()
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}({1},{2})", Name, m_propertyName, m_pattern);
            }
        }

        public static readonly PropertyMatchFunc Equal = new PropertyMatchFunc("propequal", true, false);
        public static readonly PropertyMatchFunc OptionalEqual = new PropertyMatchFunc("propeq", true, false, true);
        public static readonly PropertyMatchFunc Match = new PropertyMatchFunc("propmatch", false, false);
        public static readonly PropertyMatchFunc NotEqual = new PropertyMatchFunc("propnotequal", true, true);
        public static readonly PropertyMatchFunc NotMatch = new PropertyMatchFunc("propnotmatch", false, true);

        private bool m_exactMatch;
        private bool m_not;
        private bool m_optional;

        private PropertyMatchFunc(string name, bool exactMatch, bool not, bool optional = false)
            : base(name)
        {
            m_exactMatch = exactMatch;
            m_not = not;
            m_optional = optional;
        }

        public override object Invoke(IPropertyContext context, object[] args)
        {
            int argCount = args.Length;
            if (argCount == 0)
            {
                throw new ArgumentException("Invalid number of arguments for Match()");
            }

            string propertyName = (string) args[0];
            string pattern = (argCount > 1 && args[1] != null ? args[1].ToString() : string.Empty);
            if (pattern == null && m_optional)
            {
                return true;
            }

            object propertyValue = GetFunc.Invoke(context, propertyName);
            string property = (propertyValue != null ? propertyValue.ToString() : string.Empty);

            bool result;
            if (m_exactMatch)
            {
                result = (property == pattern);
            }
            else
            {
                result = new Regex(pattern).IsMatch(property);
            }

            return (result != m_not);
        }

        internal override GuanFunc Bind(List<GuanExpression> args)
        {
            string pattern;
            string propertyName;

            int argCount = args.Count;
            if (!m_exactMatch && args[argCount - 1].GetLiteral(out pattern))
            {
                if (!args[0].GetLiteral(out propertyName))
                {
                    return this;
                }

                args.Clear();

                return new BoundPropertyMatch(ToString(), propertyName, pattern, m_not);
            }

            return this;
        }
    }
}
