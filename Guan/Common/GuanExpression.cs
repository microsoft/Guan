// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Guan.Common
{
    public interface IGuanExpressionContext
    {
        GuanFunc GetFunc(string name);
    }

    /// <summary>
    /// Expression tree for manipulating properties.  It is usually
    /// generated from a string expression although it is also possible
    /// to manually construct the tree.
    /// Every expression tree node is basically a GuanFunc and
    /// its child nodes are the arguments to the function.
    /// Not thread safe.
    /// </summary>
    public class GuanExpression
    {
        /// <summary>
        /// Operators that can appear in the string expression.
        /// </summary>
        public class Operator
        {
            private string m_op;
            private GuanFunc m_func;
            private int m_level;

            public Operator(string op, GuanFunc func, int level)
            {
                m_op = op;
                m_func = func;
                m_level = level;
            }

            internal string Op
            {
                get
                {
                    return m_op;
                }
            }

            internal GuanFunc Func
            {
                get
                {
                    return m_func;
                }
            }

            internal int Level
            {
                get
                {
                    return m_level;
                }
            }

            internal bool Match(string exp, ref int start)
            {
                if (start + m_op.Length > exp.Length)
                {
                    return false;
                }

                for (int i = 0; i < m_op.Length; i++)
                {
                    if (m_op[i] != exp[start + i])
                    {
                        return false;
                    }
                }

                start += m_op.Length;

                return true;
            }

            public override string ToString()
            {
                return m_op;
            }
        }

        class NamedContext : IPropertyContext
        {
            private IPropertyContext original_;
            private string name_;

            public NamedContext(IPropertyContext context, string name)
            {
                original_ = context;
                name_ = name + ":";
            }

            public object this[string name]
            {
                get
                {
                    if (!name.StartsWith(name_))
                    {
                        return "<" + name + ">";
                    }

                    return original_[name.Substring(name_.Length)];
                }
            }
        }

        private GuanFunc m_func;
        private List<GuanExpression> m_children;

        private static readonly List<GuanExpression> s_nullChildren = new List<GuanExpression>();

        private static readonly Operator s_sentinel = new Operator("Dummy", Literal.Empty, 0);
        private static Tri<Operator> s_operators = CreateOperators();
        private static readonly Regex s_evalProperty = new Regex(@"(\<[\w#:.()/]+\>)", RegexOptions.Compiled);

        public GuanExpression(GuanExpression other)
        {
            m_func = other.m_func;
            if (other.m_children == s_nullChildren)
            {
                m_children = s_nullChildren;
            }
            else
            {
                m_children = new List<GuanExpression>(other.m_children.Count);
                foreach (GuanExpression child in other.m_children)
                {
                    m_children.Add(new GuanExpression(child));
                }
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="func">The function associated with this node.</param>
        public GuanExpression(GuanFunc func)
        {
            m_func = func;
            m_children = s_nullChildren;
        }

        public GuanExpression(GuanFunc func, List<GuanExpression> children)
        {
            m_func = func;
            m_children = children;
        }

        public GuanExpression(object literalValue)
        {
            m_func = new Literal(literalValue);
            m_children = s_nullChildren;
        }

        public GuanExpression Clone()
        {
            List<GuanExpression> children = new List<GuanExpression>();
            foreach (GuanExpression expression in m_children)
            {
                children.Add(expression.Clone());
            }

            return new GuanExpression(m_func, children);
        }

        /// <summary>
        /// The function for the expression.
        /// </summary>
        public GuanFunc Func
        {
            get
            {
                return m_func;
            }
        }

        /// <summary>
        /// Whether the expression is a literal value.
        /// </summary>
        public bool IsLiteral
        {
            get
            {
                return (m_func is Literal);
            }
        }

        /// <summary>
        /// The list of argument expression.
        /// </summary>
        public IList<GuanExpression> Children
        {
            get
            {
                return m_children;
            }
        }

        /// <summary>
        /// Add a child node (argument) to the expression
        /// tree node.  Note that the order of the children
        /// is usually important as it determines the order
        /// of the arguments to the function.
        /// </summary>
        /// <param name="child">The child node.</param>
        public void AddChild(GuanExpression child)
        {
            if (m_children == s_nullChildren)
            {
                m_children = new List<GuanExpression>();
            }

            m_children.Add(child);

            return;
        }

        /// <summary>
        /// Evaluate the expression within the given context.
        /// </summary>
        /// <param name="context">The context object.</param>
        /// <returns>Result of the evaluation.</returns>
        public object Evaluate(IPropertyContext context)
        {
            try
            {
                return m_func.Invoke(context, m_children);
            }
            catch (Exception e)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Error evaluating the expression {0}: {1}",
                    this, e.Message);
                throw new ExpressionException(message, e);
            }
        }

        public GuanExpression EvaluateExpression(IPropertyContext context, string contextName, bool isFinal, IGuanExpressionContext expressionContext = null)
        {
            try
            {
                string expression = (string) m_func.Invoke(new NamedContext(context, contextName), m_children);
                return GuanExpression.Build(isFinal ? expression : "{" + expression + "}", expressionContext);
            }
            catch (Exception e)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Error evaluating the expression {0}: {1}",
                    this, e.Message);
                throw new ExpressionException(message, e);
            }
        }

        public GuanPredicate EvaluatePredicate(IPropertyContext context, string contextName, IGuanExpressionContext expressionContext = null)
        {
            GuanExpression expression = EvaluateExpression(context, contextName, true, expressionContext);
            return new GuanPredicate(expression);
        }

        /// <summary>
        /// Optimize the expression by binding literal arguments to
        /// functions.
        /// </summary>
        internal void Bind()
        {
            bool isLiteral = true;

            if (m_children != s_nullChildren)
            {
                for (int i = 0; i < m_children.Count; i++)
                {
                    m_children[i].Bind();
                    isLiteral = isLiteral && (m_children[i].m_func is Literal);
                }

                m_func = m_func.Bind(m_children);
            }

            // Convert to literal if possible.
            if (isLiteral && (m_func is StandaloneFunc) && !(m_func is Literal))
            {
                m_func = new Literal(Evaluate(null));
            }

            if (m_func is Literal)
            {
                m_children = s_nullChildren;
            }
        }

        internal bool GetLiteral<T>(out T result)
        {
            if (m_func is Literal)
            {
                result = (T) Evaluate(null);
                return true;
            }
            else
            {
                result = default(T);
                return false;
            }
        }

        /// <summary>
        /// String representation of the object.
        /// </summary>
        /// <returns>String representation of the object.</returns>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder(1024);
            result.AppendFormat("{0}", m_func);

            if (m_children.Count != 0)
            {
                result.Append("(");

                foreach (GuanExpression exp in m_children)
                {
                    result.AppendFormat("{0},", exp);
                }

                result.Length--;
                result.Append(")");
            }

            return result.ToString();
        }

        private static Tri<Operator> CreateOperators()
        {
            Tri<Operator> result = new Tri<Operator>();

            AddOperator(result, new Operator("(", Literal.Empty, 1));
            AddOperator(result, new Operator(")", Literal.Empty, 1));
            AddOperator(result, new Operator("||", OrFunc.Singleton, 2));
            AddOperator(result, new Operator("&&", AndFunc.Singleton, 3));
            AddOperator(result, new Operator(">", ComparisonFunc.GT, 4));
            AddOperator(result, new Operator("<", ComparisonFunc.LT, 4));
            AddOperator(result, new Operator(">=", ComparisonFunc.GE, 4));
            AddOperator(result, new Operator("<=", ComparisonFunc.LE, 4));
            AddOperator(result, new Operator("!", NotFunc.Singleton, 7));
            AddOperator(result, new Operator("%", GetFunc.Singleton, 7));
            AddOperator(result, new Operator("~~", MatchFunc.Singleton, 4));
            AddOperator(result, new Operator("=", PropertyMatchFunc.Equal, 4));
            AddOperator(result, new Operator("==", ComparisonFunc.EQ, 4));
            AddOperator(result, new Operator("!=", ComparisonFunc.NE, 4));
            AddOperator(result, new Operator("+", AddFunc.Singleton, 5));
            AddOperator(result, new Operator("-", MathsFunc.Minus, 5));
            AddOperator(result, new Operator("*", MathsFunc.Multiply, 6));
            AddOperator(result, new Operator("/", MathsFunc.Divide, 6));

            return result;
        }

        private static void AddOperator(Tri<Operator> operators, Operator op)
        {
            operators.Add(op.Op, op);
        }

        private static void SetOperator(List<Operator> table, Operator propertyOperator)
        {
            int len = propertyOperator.Op.Length;

            int i;
            for (i = 0; i < table.Count && len <= table[i].Op.Length; i++)
            {
                if (table[i].Op == propertyOperator.Op)
                {
                    table[i] = propertyOperator;
                    return;
                }
            }

            table.Insert(i, propertyOperator);
        }

        private static Operator ReadOperator(string exp, ref int start)
        {
            return s_operators.Get(exp, ref start);
        }

        private static int SkipQuote(string exp, char quote, int index)
        {
            int i = index;
            while (++i < exp.Length)
            {
                if (exp[i] == quote)
                {
                    return i;
                }
                else if (exp[i] == '\\')
                {
                    i++;
                }
            }

            return index;
        }

        private static object ReadLiteral(string exp, char quote)
        {
            StringBuilder result = new StringBuilder(exp.Length);
            for (int i = 0; i < exp.Length; i++)
            {
                if (exp[i] != quote)
                {
                    if (exp[i] == '\\' && (i + 1) < exp.Length)
                    {
                        i++;
                        result.Append(exp[i]);
                    }
                    else
                    {
                        result.Append(exp[i]);
                    }
                }
            }

            string value = result.ToString();
            if (quote == '\0')
            {
                long intValue;
                if (long.TryParse(value, out intValue))
                {
                    return intValue;
                }
            }

            return value;
        }

        private static GuanExpression ReadFunc(string exp, string name, IGuanExpressionContext expressionContext, ref int start)
        {
            GuanFunc func = GuanFunc.Get(name, expressionContext);
            if (func == null)
            {
                throw new ArgumentException("function not found: " + name);
            }

            GuanExpression result = new GuanExpression(func);

            int level = 0;
            for (int i = start; i < exp.Length; i++)
            {
                char c = exp[i];
                if (c == '(')
                {
                    level++;
                }
                else if (c == '"' || c == '\'')
                {
                    i = SkipQuote(exp, c, i);
                }
                else if ((c == ',' || c == ')') && level == 0)
                {
                    string argExp = exp.Substring(start, i - start).Trim();
                    if (argExp.Length > 0)
                    {
                        GuanExpression child = Build(argExp, expressionContext);
                        result.AddChild(child);
                    }
                    else if (c == ',' || result.Children.Count > 0)
                    {
                        result.AddChild(new GuanExpression(new Literal(null)));
                    }

                    start = i + 1;
                }

                if (c == ')')
                {
                    if (level == 0)
                    {
                        start = i + 1;
                        return result;
                    }

                    level--;
                }
            }

            throw new ArgumentException("Invalid function expression: " + exp);
        }

        private static GuanExpression ReadToken(string exp, IGuanExpressionContext expressionContext, ref int start)
        {
            if (start >= exp.Length)
            {
                return null;
            }

            char quote = '\0';
            int i;
            for (i = start; i < exp.Length; i++)
            {
                char c = exp[i];
                // Operators are always non-alphanumerical
                if (!char.IsLetterOrDigit(c))
                {
                    if (c == '"' || c== '\'')
                    {
                        quote = c;
                        i = SkipQuote(exp, c, i);
                    }
                    else
                    {
                        if (c == '(')
                        {
                            // A '(' without leading characters is not a function
                            // but an operator.
                            string name = exp.Substring(start, i - start).Trim();
                            if (name.Length > 0)
                            {
                                start = i + 1;
                                return ReadFunc(exp, name, expressionContext, ref start);
                            }
                        }

                        if (c == '-')
                        {
                            continue;
                        }

                        int j = i;
                        if (ReadOperator(exp, ref j) != null)
                        {
                            break;
                        }
                    }
                }
            }

            string token = exp.Substring(start, i - start).Trim();
            start = i;

            if (token.Length == 0)
            {
                return null;
            }

            return new GuanExpression(ReadLiteral(token, quote));
        }

        private static void Process(Stack<Operator> operators,
                                    Stack<GuanExpression> tokens)
        {
            Operator op = operators.Pop();
            GuanExpression result = new GuanExpression(op.Func);

            if (tokens.Count < 2)
            {
                throw new ArgumentException("Expression not well formed");
            }

            GuanExpression token2 = tokens.Pop();
            GuanExpression token1 = tokens.Pop();

            if (token1 != null)
            {
                result.AddChild(token1);
            }

            if (token2 != null)
            {
                result.AddChild(token2);
            }

            tokens.Push(result);
        }

        private static GuanExpression PrivateBuild(string exp, IGuanExpressionContext expressionContext)
        {
            Stack<Operator> operators = new Stack<Operator>();
            Stack<GuanExpression> tokens = new Stack<GuanExpression>();

            operators.Push(s_sentinel);

            // We consider the expression to be made of token and operators
            // alternatively.  For unary operator, we will consider there
            // to be a null token between it and the other token.  Similarly
            // for operator that does not have any argument, we consider it
            // to have two null tokens around it.
            // "(" is a special case where we will remove the preceding null
            // token immediately before we push it on operator stack.
            // Also when ")" is processed, we look for a next operator instead
            // of null token.  It can be considered that the null token after
            // ")" is consumed immediately.

            int start = 0;
            bool isToken = true;

            // The expression is considered to end with a token (can be null)
            // so the loop always exit when searching for an operator while
            // reaching the end of the expression.
            while (isToken || start < exp.Length)
            {
                if (isToken)
                {
                    GuanExpression token = ReadToken(exp, expressionContext, ref start);
                    tokens.Push(token);
                }
                else
                {
                    // Filter white space when matching operators.
                    while (start < exp.Length && char.IsWhiteSpace(exp[start]))
                    {
                        start++;
                    }

                    Operator op = ReadOperator(exp, ref start);
                    if (op == null)
                    {
                        string msg = string.Format(CultureInfo.InvariantCulture,
                                                   "Expression {0} not well formed. Unrecognized operator: {1}",
                                                   exp, exp.Substring(start));

                        throw new ArgumentException(msg);
                    }

                    if (op.Op == "(")
                    {
                        GuanExpression dummy = tokens.Pop();
                        ReleaseAssert.IsTrue(dummy == null,
                                             "Expression: {0}", exp);

                        operators.Push(op);
                    }
                    else if (op.Op == ")")
                    {
                        while (operators.Peek().Op != "(")
                        {
                            Process(operators, tokens);
                        }

                        operators.Pop();
                        isToken = true;
                    }
                    else
                    {
                        // Process the higher priority operators on the stack.
                        while (operators.Peek().Level >= op.Level)
                        {
                            Process(operators, tokens);
                        }

                        operators.Push(op);
                    }
                }

                isToken = !isToken;
            }

            // Leave the sential
            while (operators.Count > 1)
            {
                Process(operators, tokens);
            }

            ReleaseAssert.IsTrue(tokens.Count == 1,
                                 "Expression: {0}", exp);

            GuanExpression result = tokens.Pop();
            result.Bind();

            return result;
        }

        /// <summary>
        /// Build a trace expression from a string.
        /// </summary>
        /// <param name="exp">The string expression.</param>
        /// <returns>The trace expression built from the string.</returns>
        public static GuanExpression Build(string exp, IGuanExpressionContext expressionContext = null)
        {
            if (string.IsNullOrEmpty(exp))
            {
                return null;
            }

            if (exp.StartsWith("{", StringComparison.Ordinal) && exp.EndsWith("}", StringComparison.Ordinal))
            {
                if (exp.StartsWith("{{", StringComparison.Ordinal) && exp.EndsWith("}}", StringComparison.Ordinal))
                {
                    exp = "expression(\"" + s_evalProperty.Replace(exp.Substring(2, exp.Length - 4), EvalProperty) + "\")";
                }
                else
                {
                    exp = "add(\"" + s_evalProperty.Replace(exp.Substring(1, exp.Length - 2), EvalProperty) + "\")";
                }
            }

            try
            {
                return PrivateBuild(exp, expressionContext);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Invalid expression: " + exp, e);
            }
        }

        public static GuanExpression BuildGetExpression(string variable)
        {
            List<GuanExpression> child = new List<GuanExpression>(1)
            {
                new GuanExpression(variable)
            };
            return new GuanExpression(GetFunc.Singleton, child);
        }

        public static GuanFunc GetGuanFunc(string name)
        {
            GuanFunc result = GuanFunc.Get(name, null);
            if (result == null)
            {
                Operator op = s_operators.Get(name);
                if (op != null)
                {
                    result = op.Func;
                }
            }

            return result;
        }

        public bool IsSimpleGetExpression()
        {
            return (m_func == GetFunc.Singleton && m_children.Count == 1 && m_children[0].IsLiteral);
        }

        private static string EvalProperty(Match match)
        {
            string name = match.Groups[1].Value;
            name = name.Substring(1, name.Length - 2);

            if (name.Contains("(") && name.Contains(")"))
            {
                return "\"," + name + ",\"";
            }

            return "\",get(" + name + "),\"";
        }

        public List<string> GetContextVariables()
        {
            List<string> result = new List<string>();
            GetContextVariable(result);

            return result;
        }

        private void GetContextVariable(List<string> result)
        {
            for (int i = 0; i < m_children.Count; i++)
            {
                GuanExpression child = m_children[i];
                if (m_func is GetFunc && i == 0 && child.m_func is Literal)
                {
                    string variable = (string)child.Evaluate(null);
                    if (!result.Contains(variable))
                    {
                        result.Add(variable);
                    }
                }
                else
                {
                    child.GetContextVariable(result);
                }
            }
        }

        public void ChangeContextVariable(string oldName, string newName)
        {
            foreach (GuanExpression child in m_children)
            {
                child.ChangeContextVariable(oldName, newName);

                if (m_func is GetFunc && child.m_func is Literal && oldName == (string)child.Evaluate(null))
                {
                    child.m_func = new Literal(newName);
                }
            }
        }

        public void ChangeContextVariables(Dictionary<string, string> namePairs)
        {
            foreach (GuanExpression child in m_children)
            {
                child.ChangeContextVariables(namePairs);

                if (m_func is GetFunc && child.m_func is Literal)
                {
                    string oldName = (string)child.Evaluate(null);
                    string newName;
                    if (namePairs.TryGetValue(oldName, out newName))
                    {
                        child.m_func = new Literal(newName);
                    }
                }
            }
        }

        public void ReplaceContextVariable(string oldVariable, object value)
        {
            foreach (GuanExpression child in m_children)
            {
                if (m_func is GetFunc && child.m_func is Literal && (string)child.Evaluate(null) == oldVariable)
                {
                    m_func = new Literal(value);
                    m_children = s_nullChildren;
                }
                else
                {
                    child.ReplaceContextVariable(oldVariable, value);
                }
            }
        }
    }

    [Serializable]
    internal class ExpressionException : GuanException
    {
        public ExpressionException(string message)
            : base(message)
        {
        }

        public ExpressionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
