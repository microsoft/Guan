using System.Collections;
using System.Text;
using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// Logic specific to list.
    /// </summary>
    public static class ListTerm
    {
        public static Functor ListFunctor = new Functor(".");

        public static CompoundTerm Add(CompoundTerm current, Term child)
        {
            if (current.Arguments.Count == 0)
            {
                current.AddArgument(child, "0");
                return current;
            }

            ReleaseAssert.IsTrue(current.Arguments.Count == 1);
            CompoundTerm tail = new CompoundTerm(ListFunctor, null);
            current.AddArgument(tail, "1");
            tail.AddArgument(child, "0");
            return tail;
        }

        public static string ToString(CompoundTerm term)
        {
            StringBuilder result = new StringBuilder();
            result.Append("[");

            CompoundTerm current = term;
            Term next;
            do
            {
                result.Append(current.Arguments[0].Value).Append(',');
                next = current.Arguments[1].Value.GetEffectiveTerm();
                current = next as CompoundTerm;
            } while (current != null && current.Functor.Name == ".");

            result.Length--;
            if (next == Constant.Nil)
            {
                result.Append("]");
            }
            else
            {
                result.AppendFormat("|{0}]", next);
            }

            return result.ToString();
        }

        public static Term Parse(CompoundTerm term)
        {
            if (term.Arguments.Count == 0)
            {
                return Constant.Nil;
            }

            Term head = term.Arguments[0].Value;
            Term tail;

            ReleaseAssert.IsTrue(term.Arguments.Count == 1);
            CompoundTerm compound = head as CompoundTerm;
            if (compound != null && compound.Functor.Name == "|")
            {
                ReleaseAssert.IsTrue(compound.Arguments.Count == 2);
                head = compound.Arguments[0].Value;
                tail = compound.Arguments[1].Value;
            }
            else
            {
                tail = Constant.Nil;
            }

            CompoundTerm result = new CompoundTerm(ListTerm.ListFunctor, VariableBinding.Ground);
            CompoundTerm current = result;
            while (head != null)
            {
                compound = head as CompoundTerm;
                if (compound != null && compound.Functor.Name == ",")
                {
                    ReleaseAssert.IsTrue(compound.Arguments.Count == 2);
                    current.AddArgument(compound.Arguments[0].Value, "0");
                    head = compound.Arguments[1].Value;
                    CompoundTerm next = new CompoundTerm(ListTerm.ListFunctor, VariableBinding.Ground);
                    current.AddArgument(next, "1");
                    current = next;
                }
                else
                {
                    current.AddArgument(head, "0");
                    head = null;
                }
            }

            current.AddArgument(tail, "1");

            return result;
        }

        public static Term FromEnumerable(IEnumerable collection)
        {
            Term result = Constant.Nil;
            CompoundTerm current = null;
            foreach (object member in collection)
            {
                CompoundTerm next = new CompoundTerm(ListFunctor);
                if (current != null)
                {
                    current.AddArgument(next, "1");
                }
                else
                {
                    result = next;
                }

                current = next;
                current.AddArgument(Term.FromObject(member), "0");
            }

            if (current != null)
            {
                current.AddArgument(Constant.Nil, "1");
            }

            return result;
        }
    }
}
