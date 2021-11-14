///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.
//
// @File: GuanPredicate.cs
//
// @Owner: xunlu
// @Test:  xunlu
//
// Purpose:
//   Predicate expression used for matching trace records.
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Guan.Logic
{
    using System;

    public class GuanPredicate
    {
        public static readonly GuanPredicate MatchAll = new GuanPredicate(true);
        public static readonly GuanPredicate MatchNothing = new GuanPredicate(false);

        private GuanExpression exp;

        public GuanPredicate(GuanExpression exp)
        {
            this.exp = exp;
        }

        private GuanPredicate(bool matchAll)
        {
            this.exp = new GuanExpression(matchAll ? Literal.True : Literal.False);
        }

        public GuanExpression Expression
        {
            get
            {
                return this.exp;
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

        public GuanPredicate Clone()
        {
            return new GuanPredicate(this.exp.Clone());
        }

        public bool Match(IPropertyContext context)
        {
            if (this.exp == null)
            {
                return true;
            }

            object result = this.exp.Evaluate(context);
            if (result == null)
            {
                return false;
            }

            if (result is bool)
            {
                return (bool)result;
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
                return this.Match(context);
            }
            catch (GuanExpression.ExpressionException)
            {
                return false;
            }
        }

        public override string ToString()
        {
            if (this.exp == null)
            {
                return "MatchAll";
            }

            return this.exp.ToString();
        }
    }
}
