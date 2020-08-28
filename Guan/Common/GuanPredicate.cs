// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Guan.Common
{
    public class GuanPredicate
    {
        private GuanExpression m_exp;

        public static readonly GuanPredicate MatchAll = new GuanPredicate(true);
        public static readonly GuanPredicate MatchNothing = new GuanPredicate(false);

        private GuanPredicate(bool matchAll)
        {
            m_exp = new GuanExpression(matchAll ? Literal.True : Literal.False);
        }

        public GuanPredicate(GuanExpression exp)
        {
            m_exp = exp;
        }

        public GuanPredicate Clone()
        {
            return new GuanPredicate(m_exp.Clone());
        }

        public GuanExpression Expression
        {
            get
            {
                return m_exp;
            }
        }

        public bool Match(IPropertyContext context)
        {
            if (m_exp == null)
            {
                return true;
            }

            object result = m_exp.Evaluate(context);
            if (result == null)
            {
                return false;
            }

            if (result is bool)
            {
                return (bool) result;
            }

            string s = result as string;
            if (s != null)
            {
                return s.Length > 0;
            }

            return true;
        }

        public bool SafeMatch(IPropertyContext context)
        {
            try
            {
                return Match(context);
            }
            catch (ExpressionException)
            {
                return false;
            }
        }

        public static GuanPredicate Build(string exp, IGuanExpressionContext expressionContext = null)
        {
            GuanExpression expression = GuanExpression.Build(exp, expressionContext);
            GuanPredicate result = new GuanPredicate(expression);
            if (expression.IsLiteral)
            {
                result = (result.Match(null) ? MatchAll : MatchNothing);
            }

            return result;
        }

        public override string ToString()
        {
            if (m_exp == null)
            {
                return "MatchAll";
            }

            return m_exp.ToString();
        }
    }
}
