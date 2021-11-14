//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Parser for term expressions.s
    /// </summary>
    internal static class TermExpression
    {
        private static readonly OperatorTri Operators = CreateOperators();
        private static readonly Functor IdentityFunctor = new Functor("()");
        private static readonly Operator CommaOperator = Operators.Get(",");
        private static readonly Operator CloseParentheses = Operators.Get(")");

        private enum Associativity
        {
            Left,
            Right,
            None,
            Open,
            Close,
        }

        public static Term Parse(string text)
        {
            // Conceptually, last is the top of the stack. It is separated from
            // the stack for convenient access since we often need to examine
            // the top item.
            Stack<CompoundTerm> pending = new Stack<CompoundTerm>();
            CompoundTerm last = new CompoundTerm(IdentityFunctor);
            Term term = null;

            int i = 0;
            while (i <= text.Length)
            {
                CompoundTerm current = null;
                string token;
                Operator op = Read(text, ref i, out token);

                if (op != null)
                {
                    if (op.Name == "(")
                    {
                        if (token != null)
                        {
                            current = new CompoundTerm(new Functor(token));
                            token = null;
                        }
                        else
                        {
                            current = new CompoundTerm(IdentityFunctor);
                        }

                        op = null;
                    }
                    else if (op.Associativity != Associativity.Close)
                    {
                        current = new CompoundTerm(op.Func);
                        if (op.Associativity == Associativity.Open)
                        {
                            op = null;
                        }
                    }
                }

                if (token != null)
                {
                    if (term != null)
                    {
                        throw new GuanException("Not well formed at {0} {1}", term, token);
                    }

                    term = Constant.Parse(token);
                }

                while (op != null && CanPop(last.Functor, ref op))
                {
                    if (term != null)
                    {
                        AddArgument(last, term);
                    }

                    // Normalize "(term)" to "term".
                    if (op == null && last.Functor == IdentityFunctor && last.Arguments.Count == 1)
                    {
                        CompoundTerm temp = last.Arguments[0].Value as CompoundTerm;
                        if (temp != null)
                        {
                            last = temp;
                        }
                    }

                    if (current != null && current.Functor.Name == "," && op == null)
                    {
                        current = null;
                        term = null;
                    }
                    else
                    {
                        term = last;
                        if (pending.Count > 0)
                        {
                            last = pending.Pop();
                        }
                        else
                        {
                            ReleaseAssert.IsTrue(i >= text.Length, "Expression {0} not well formed", text);
                        }
                    }
                }

                if (current != null)
                {
                    if (term != null)
                    {
                        AddArgument(current, term);
                        term = null;
                    }

                    pending.Push(last);
                    last = current;
                }
            }

            ReleaseAssert.IsTrue(pending.Count == 0);

            return ConvertIdentity(term);
        }

        private static OperatorTri CreateOperators()
        {
            OperatorTri result = new OperatorTri();

            result.Add("(", 99, Associativity.Open, "()");
            result.Add(")", 99, Associativity.Close);
            result.Add("[", 99, Associativity.Open);
            result.Add("]", 99, Associativity.Close);

            result.Add(":-", 89, Associativity.None);

            result.Add(";", 79, Associativity.Right);
            result.Add("|", 79, Associativity.Right);
            result.Add(",", 78, Associativity.Right, "','");

            result.Add("=", 69, Associativity.None);
            result.Add("is", 69, Associativity.None);

            result.Add("||", 49);
            result.Add("&&", 48);

            result.Add("==", 39, Associativity.None);
            result.Add("!=", 39, Associativity.None);
            result.Add(">", 38, Associativity.None);
            result.Add(">=", 38, Associativity.None);
            result.Add("<", 38, Associativity.None);
            result.Add("<=", 38, Associativity.None);

            result.Add("+", 19);
            result.Add("-", 19);
            result.Add("*", 18);
            result.Add("/", 18);
            result.Add("%", 18);

            return result;
        }

        private static void AddArgument(CompoundTerm term, Term arg)
        {
            term.AddArgument(ConvertIdentity(arg), term.Arguments.Count.ToString());
        }

        private static Term ConvertIdentity(Term arg)
        {
            CompoundTerm compoundArg = arg as CompoundTerm;
            while (compoundArg != null && compoundArg.Functor == IdentityFunctor)
            {
                ReleaseAssert.IsTrue(compoundArg.Arguments.Count == 1);
                arg = compoundArg.Arguments[0].Value;
                compoundArg = arg as CompoundTerm;
            }

            return arg;
        }

        /// <summary>
        /// Whether the stack should be popped.
        /// </summary>
        /// <param name="last">Functor of the top item on the (conceptual) stack.</param>
        /// <param name="current">The current operator, will be set to null if it
        /// is consumed by the last term.</param>
        /// <returns>True if the stack should pop.</returns>
        private static bool CanPop(Functor last, ref Operator current)
        {
            OperatorFunctor lastOp = last as OperatorFunctor;

            if (current.Name == ")")
            {
                if (lastOp == null)
                {
                    current = null;
                }

                return true;
            }

            if (current.Name == "]")
            {
                if (lastOp != null && lastOp.Name == "[")
                {
                    current = null;
                }

                return true;
            }

            if (lastOp == null)
            {
                if (current.Name != "," || last == IdentityFunctor)
                {
                    return false;
                }

                current = null;
                return true;
            }

            if (lastOp.Operator.Priority < current.Priority)
            {
                return true;
            }

            if (lastOp.Operator.Priority > current.Priority)
            {
                return false;
            }

            if (current.Associativity == Associativity.Left && lastOp.Operator.Associativity != Associativity.Right)
            {
                return true;
            }

            if (current.Associativity == Associativity.Left || lastOp.Operator.Associativity != Associativity.Right)
            {
                throw new GuanException("Invalid combination of operators {0} and {1}", lastOp.Operator, current);
            }

            return false;
        }

        /// <summary>
        /// Read the next operator.
        /// </summary>
        /// <param name="text">The entire expression.</param>
        /// <param name="offset">Start index in the expression for reading.</param>
        /// <param name="token">The token before the operator, if any.</param>
        /// <returns>The next operator, null if a token followed by white space is read.</returns>
        private static Operator Read(string text, ref int offset, out string token)
        {
            int start = offset;
            while (start < text.Length && char.IsWhiteSpace(text[start]))
            {
                start++;
            }

            Operator op = null;
            token = null;
            offset = start;
            while (offset < text.Length && !char.IsWhiteSpace(text[offset]) && op == null)
            {
                char c = text[offset];
                if (c == '"' || c == '\'')
                {
                    if (offset != start)
                    {
                        throw new GuanException("Invalid quote in term expression: {0}", text);
                    }

                    int end = SkipQuote(text, c, offset);
                    token = UnescapeQuote(text.Substring(start, end - start + 1), c);
                    offset = end + 1;
                }
                else
                {
                    if (c == '_' || char.IsLetterOrDigit(c))
                    {
                        // check for operators like "is"
                        if (offset == start)
                        {
                            op = Operators.Get(text, ref offset);
                            // If followed by a non-symbol character, consider it as word prefix instead of operator
                            if (op != null && offset < text.Length && (text[offset] == '_' || char.IsLetterOrDigit(text[offset])))
                            {
                                op = null;
                                offset = start;
                            }
                        }
                    }
                    else
                    {
                        op = Operators.Get(text, ref offset);
                    }

                    if (op == null)
                    {
                        if (token != null)
                        {
                            throw new GuanException("Invalid quote in term expression: {0}", text);
                        }

                        offset++;
                    }
                }
            }

            if (token == null)
            {
                int tokenEnd = (op != null ? offset - op.Name.Length : offset);
                if (tokenEnd > start)
                {
                    token = text.Substring(start, tokenEnd - start);
                }
            }

            // When the end of expression is reached, add a ')' to balance to the initial
            // '(' introduced when parsing is started.
            if (op == null && offset == text.Length)
            {
                op = CloseParentheses;
                offset++;
            }

            return op;
        }

        private static int SkipQuote(string text, char quote, int index)
        {
            int i = index;
            while (++i < text.Length)
            {
                if (text[i] == quote)
                {
                    return i;
                }
                else if (text[i] == '\\')
                {
                    i++;
                }
            }

            return index;
        }

        private static string UnescapeQuote(string text, char quote)
        {
            string to = quote.ToString();
            string from = "\\" + to;
            return text.Replace(from, to);
        }

        private class Operator
        {
            private string display;

            public Operator(string name, int priority, Associativity associativity, string display)
            {
                this.Name = name;
                this.Priority = priority;
                this.Associativity = associativity;
                this.Func = new OperatorFunctor(this);
                this.display = display;
            }

            public string Name { get; set; }

            public int Priority { get; set; }

            public Associativity Associativity { get; set; }

            public OperatorFunctor Func { get; set; }

            public CompoundTerm CreateTerm()
            {
                return new CompoundTerm(this.Func);
            }

            public override string ToString()
            {
                return this.display;
            }
        }

        private class OperatorFunctor : Functor
        {
            public OperatorFunctor(Operator op)
                : base(op.Name)
            {
                this.Operator = op;
            }

            public Operator Operator { get; set; }

            public override string ToString()
            {
                return this.Operator.ToString();
            }
        }

        private class OperatorTri : Tri<Operator>
        {
            public void Add(string name, int priority, Associativity associativity = Associativity.Left, string display = null)
            {
                this.Add(name, new Operator(name, priority, associativity, display ?? name));
            }
        }
    }
}
