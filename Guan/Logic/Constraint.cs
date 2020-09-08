using System;
using System.Collections.Generic;
using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// A constraint is a collection of criteria (each represented as an evalutable
    /// compound) that can be validated.
    /// Other than the criteria, separate entries for upper/lower bound and possible
    /// values are maintained so that the constraint can be used not only for passive
    /// validation, but also actively queried by the resolvers for more efficient
    /// search.
    /// </summary>
    public class Constraint
    {
        class BoundEntry
        {
            private CompoundTerm term_;
            private Variable variable_;
            private object value_;
            private bool isInclusive_;

            public BoundEntry(CompoundTerm term, Variable variable, object value, bool isInclusive)
            {
                term_ = term;
                variable_ = variable;
                value_ = value;
                isInclusive_ = isInclusive;
            }

            public CompoundTerm Term
            {
                get
                {
                    return term_;
                }
            }

            public object Get(Variable variable, out bool isInclusive)
            {
                //TODO: this is probably does not handle all the cases.
                if (GetEffectiveTerm(variable) != GetEffectiveTerm(variable_))
                {
                    isInclusive = false;
                    return null;
                }

                isInclusive = isInclusive_;
                return value_;
            }

            private static Term GetEffectiveTerm(Variable variable)
            {
                Term term = variable.GetEffectiveTerm();
                OutputVariable outputVariable = term as OutputVariable;
                if (outputVariable != null)
                {
                    return GetEffectiveTerm(outputVariable.Original);
                }

                return term;
            }
        }

        class ValueEntry
        {
            private CompoundTerm term_;
            private Variable variable_;
            private List<object> values_;

            public ValueEntry(CompoundTerm term)
            {
                term_ = term;
                values_ = new List<object>();
            }

            public ValueEntry(CompoundTerm term, Variable variable, object value)
            {
                term_ = term;
                variable_ = variable;
                values_ = new List<object>(1);
                values_.Add(value);
            }

            public CompoundTerm Term
            {
                get
                {
                    return term_;
                }
            }

            public bool Add(RawEntry entry)
            {
                if (entry.Func != ComparisonFunc.EQ)
                {
                    return false;
                }

                if (variable_ == null)
                {
                    variable_ = entry.Variable;
                }
                else if (variable_ != entry.Variable)
                {
                    return false;
                }

                values_.Add(entry.Value);
                return true;
            }

            public List<object> Get(Variable variable)
            {
                if (variable != variable_.GetEffectiveTerm())
                {
                    return null;
                }

                return values_;
            }
        }

        /// <summary>
        /// Terms like "variable comparison constant" or "constant comparison variable" only
        /// </summary>
        class RawEntry
        {
            private Variable variable_;
            private object value_;
            private ComparisonFunc func_;

            private RawEntry(Variable variable, object value, ComparisonFunc func)
            {
                variable_ = variable;
                value_ = value;
                func_ = func;
            }

            public Variable Variable
            {
                get
                {
                    return variable_;
                }
            }

            public object Value
            {
                get
                {
                    return value_;
                }
            }

            public ComparisonFunc Func
            {
                get
                {
                    return func_;
                }
            }

            public void AddToConstaint(Constraint constraint, CompoundTerm term)
            {
                if (func_ == ComparisonFunc.EQ)
                {
                    constraint.AddValueEntry(new ValueEntry(term, variable_, value_));
                }
                else if (func_ == ComparisonFunc.LT || func_ == ComparisonFunc.LE)
                {
                    constraint.AddUpperBoundEntry(new BoundEntry(term, variable_, value_, func_ == ComparisonFunc.LE));
                }
                else if (func_ == ComparisonFunc.GT || func_ == ComparisonFunc.GE)
                {
                    constraint.AddLowerBoundEntry(new BoundEntry(term, variable_, value_, func_ == ComparisonFunc.GE));
                }
            }

            public static RawEntry Convert(CompoundTerm term)
            {
                EvaluatedFunctor evaluatedFunctor = term.GetEvaluatedFunctor();
                if (evaluatedFunctor == null || term.Arguments.Count != 2)
                {
                    return null;
                }

                ComparisonFunc func = evaluatedFunctor.Func as ComparisonFunc;
                if (func == null)
                {
                    return null;
                }

                Term term1 = term.Arguments[0].Value.GetEffectiveTerm();
                Term term2 = term.Arguments[1].Value.GetEffectiveTerm();

                Constant constant = null;
                Variable variable = term1 as Variable;
                if (variable != null)
                {
                    constant = term2 as Constant;
                }
                else
                {
                    constant = term1 as Constant;
                    variable = term2 as Variable;
                    func = func.Inverse();
                }

                if (variable == null || constant == null)
                {
                    return null;
                }

                return new RawEntry(variable, constant.Value, func);
            }
        }

        private List<CompoundTerm> terms_;
        private List<BoundEntry> lowerEntries_;
        private List<BoundEntry> upperEnties_;
        private List<ValueEntry> valueEntries_;

        public static readonly Constraint Empty = new Constraint();

        internal Constraint()
        {
            terms_ = new List<CompoundTerm>();
        }

        internal List<CompoundTerm> Terms
        {
            get
            {
                return terms_;
            }
        }

        internal void Add(CompoundTerm term)
        {
            Process(term);
        }

        internal void Add(IEnumerable<CompoundTerm> terms)
        {
            foreach (CompoundTerm term in terms)
            {
                Process(term);
            }
        }

        internal int Evaluate(QueryContext context)
        {
            int result = 1;
            foreach (CompoundTerm term in terms_)
            {
                Constant evaluted = term.Evaluate(context) as Constant;
                if (evaluted == null)
                {
                    result = 0;
                }
                else if (!evaluted.IsTrue())
                {
                    return -1;
                }
            }

            return result;
        }

        private void Process(CompoundTerm term)
        {
            EvaluatedFunctor evaluatedFunctor = term.GetEvaluatedFunctor();

            bool add = true;
            if (evaluatedFunctor != null)
            {
                if (evaluatedFunctor.Func is AndFunc)
                {
                    foreach (var arg in term.Arguments)
                    {
                        CompoundTerm compound = arg.Value as CompoundTerm;
                        if (compound != null)
                        {
                            Process(compound);
                        }
                    }

                    add = false;
                }

                if (evaluatedFunctor.Func is OrFunc)
                {
                    ValueEntry entry = new ValueEntry(term);
                    // We will only handle disjunction of a collectoin of comparisons
                    // with the same variable.
                    if (DecomposeDisjunction(term, entry))
                    {
                        AddValueEntry(entry);
                        add = false;
                    }
                }
                else
                {
                    RawEntry rawEntry = RawEntry.Convert(term);
                    if (rawEntry != null)
                    {
                        rawEntry.AddToConstaint(this, term);
                    }
                }
            }

            if (add)
            {
                terms_.Add(term);
            }
        }

        private bool DecomposeDisjunction(CompoundTerm term, ValueEntry entry)
        {
            foreach (var arg in term.Arguments)
            {
                CompoundTerm compound = arg.Value as CompoundTerm;
                if (compound == null)
                {
                    return false;
                }

                EvaluatedFunctor evaluatedFunctor = compound.GetEvaluatedFunctor();
                if (evaluatedFunctor.Func is OrFunc)
                {
                    if (!DecomposeDisjunction(compound, entry))
                    {
                        return false;
                    }
                }
                else
                {
                    RawEntry rawEntry = RawEntry.Convert(compound);
                    if (rawEntry == null || !entry.Add(rawEntry))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void AddValueEntry(ValueEntry entry)
        {
            if (valueEntries_ == null)
            {
                valueEntries_ = new List<ValueEntry>();
            }

            valueEntries_.Add(entry);
        }

        private void AddUpperBoundEntry(BoundEntry entry)
        {
            if (upperEnties_ == null)
            {
                upperEnties_ = new List<BoundEntry>();
            }

            upperEnties_.Add(entry);
        }

        private void AddLowerBoundEntry(BoundEntry entry)
        {
            if (lowerEntries_ == null)
            {
                lowerEntries_ = new List<BoundEntry>();
            }

            lowerEntries_.Add(entry);
        }

        public List<object> GetValues(Variable variable, bool remove)
        {
            if (valueEntries_ == null)
            {
                return null;
            }

            List<object> result = null;
            for (int i = valueEntries_.Count - 1; i >= 0; i--)
            {
                List<object> values = valueEntries_[i].Get(variable);
                if (values != null)
                {
                    if (result == null)
                    {
                        result = values;
                    }
                    else
                    {
                        MergeValues(result, values);
                    }

                    if (remove)
                    {
                        terms_.Remove(valueEntries_[i].Term);
                        valueEntries_.RemoveAt(i);
                    }
                }
            }

            return result;
        }

        private static void MergeValues(List<object> list1, List<object> list2)
        {
            for (int i = list1.Count - 1; i >= 0; i--)
            {
                bool found = false;
                for (int j = 0; j < list2.Count && !found; j++)
                {
                    found = object.Equals(list1[i], list2[j]);
                }

                if (!found)
                {
                    list1.RemoveAt(i);
                }
            }
        }

        public object GetLowerBound(Variable variable, bool remove, out bool isInclusive)
        {
            return GetBound(false, variable, remove, out isInclusive);
        }

        public object GetUpperBound(Variable variable, bool remove, out bool isInclusive)
        {
            return GetBound(true, variable, remove, out isInclusive);
        }

        private object GetBound(bool upper, Variable variable, bool remove, out bool isInclusive)
        {
            isInclusive = false;

            List<BoundEntry> entries = (upper ? upperEnties_ : lowerEntries_);
            if (entries == null)
            {
                return null;
            }

            object result = null; 
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                bool inclusive;
                object value = entries[i].Get(variable, out inclusive);
                if (value != null)
                {
                    if (result == null || CompareResult(value, inclusive, result, isInclusive, upper))
                    {
                        result = value;
                        isInclusive = inclusive;
                    }

                    if (remove)
                    {
                        terms_.Remove(entries[i].Term);
                        entries.RemoveAt(i);
                    }
                }
            }

            return result;
        }

        private bool CompareResult(object value1, bool inclusive1, object value2, bool inclusive2, bool upper)
        {
            if (object.Equals(value1, value2))
            {
                return (!inclusive1 && inclusive2);
            }

            ComparisonFunc func = (upper ? ComparisonFunc.LT : ComparisonFunc.GT);
            return func.Invoke(value1, value2);
        }
    }
}
