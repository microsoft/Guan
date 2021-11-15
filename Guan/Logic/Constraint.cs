// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System.Collections.Generic;

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
        public static readonly Constraint Empty = new Constraint();

        private List<CompoundTerm> terms;
        private List<BoundEntry> lowerEntries;
        private List<BoundEntry> upperEnties;
        private List<ValueEntry> valueEntries;

        internal Constraint()
        {
            this.terms = new List<CompoundTerm>();
        }

        internal List<CompoundTerm> Terms
        {
            get
            {
                return this.terms;
            }
        }

        public List<object> GetValues(Variable variable, bool remove)
        {
            if (this.valueEntries == null)
            {
                return null;
            }

            List<object> result = null;
            for (int i = this.valueEntries.Count - 1; i >= 0; i--)
            {
                List<object> values = this.valueEntries[i].Get(variable);
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
                        _ = this.terms.Remove(this.valueEntries[i].Term);
                        this.valueEntries.RemoveAt(i);
                    }
                }
            }

            return result;
        }

        public object GetLowerBound(Variable variable, bool remove, out bool isInclusive)
        {
            return this.GetBound(false, variable, remove, out isInclusive);
        }

        public object GetUpperBound(Variable variable, bool remove, out bool isInclusive)
        {
            return this.GetBound(true, variable, remove, out isInclusive);
        }

        internal void Add(CompoundTerm term)
        {
            this.Process(term);
        }

        internal void Add(IEnumerable<CompoundTerm> terms)
        {
            foreach (CompoundTerm term in terms)
            {
                this.Process(term);
            }
        }

        internal int Evaluate(QueryContext context)
        {
            int result = 1;
            foreach (CompoundTerm term in this.terms)
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

        private void Process(CompoundTerm term)
        {
            EvaluatedFunctor evaluatedFunctor = term.GetEvaluatedFunctor();

            bool add = true;
            if (evaluatedFunctor != null)
            {
                if (evaluatedFunctor.Func is AndFunc)
                {
                    foreach (TermArgument arg in term.Arguments)
                    {
                        CompoundTerm compound = arg.Value as CompoundTerm;
                        if (compound != null)
                        {
                            this.Process(compound);
                        }
                    }

                    add = false;
                }

                if (evaluatedFunctor.Func is OrFunc)
                {
                    ValueEntry entry = new ValueEntry(term);
                    // We will only handle disjunction of a collectoin of comparisons
                    // with the same variable.
                    if (this.DecomposeDisjunction(term, entry))
                    {
                        this.AddValueEntry(entry);
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
                this.terms.Add(term);
            }
        }

        private bool DecomposeDisjunction(CompoundTerm term, ValueEntry entry)
        {
            foreach (TermArgument arg in term.Arguments)
            {
                CompoundTerm compound = arg.Value as CompoundTerm;
                if (compound == null)
                {
                    return false;
                }

                EvaluatedFunctor evaluatedFunctor = compound.GetEvaluatedFunctor();
                if (evaluatedFunctor.Func is OrFunc)
                {
                    if (!this.DecomposeDisjunction(compound, entry))
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
            if (this.valueEntries == null)
            {
                this.valueEntries = new List<ValueEntry>();
            }

            this.valueEntries.Add(entry);
        }

        private void AddUpperBoundEntry(BoundEntry entry)
        {
            if (this.upperEnties == null)
            {
                this.upperEnties = new List<BoundEntry>();
            }

            this.upperEnties.Add(entry);
        }

        private void AddLowerBoundEntry(BoundEntry entry)
        {
            if (this.lowerEntries == null)
            {
                this.lowerEntries = new List<BoundEntry>();
            }

            this.lowerEntries.Add(entry);
        }

        private object GetBound(bool upper, Variable variable, bool remove, out bool isInclusive)
        {
            isInclusive = false;

            List<BoundEntry> entries = (upper ? this.upperEnties : this.lowerEntries);
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
                    if (result == null || this.CompareResult(value, inclusive, result, isInclusive, upper))
                    {
                        result = value;
                        isInclusive = inclusive;
                    }

                    if (remove)
                    {
                        _ = this.terms.Remove(entries[i].Term);
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

        private class BoundEntry
        {
            private CompoundTerm term;
            private Variable variable;
            private object value;
            private bool isInclusive;

            public BoundEntry(CompoundTerm term, Variable variable, object value, bool isInclusive)
            {
                this.term = term;
                this.variable = variable;
                this.value = value;
                this.isInclusive = isInclusive;
            }

            public CompoundTerm Term
            {
                get
                {
                    return this.term;
                }
            }

            public object Get(Variable variable, out bool isInclusive)
            {
                //TODO: this is probably does not handle all the cases.
                if (GetEffectiveTerm(variable) != GetEffectiveTerm(this.variable))
                {
                    isInclusive = false;
                    return null;
                }

                isInclusive = this.isInclusive;
                return this.value;
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

        private class ValueEntry
        {
            private CompoundTerm term;
            private Variable variable;
            private List<object> values;

            public ValueEntry(CompoundTerm term)
            {
                this.term = term;
                this.values = new List<object>();
            }

            public ValueEntry(CompoundTerm term, Variable variable, object value)
            {
                this.term = term;
                this.variable = variable;
                this.values = new List<object>(1)
                {
                    value
                };
            }

            public CompoundTerm Term
            {
                get
                {
                    return this.term;
                }
            }

            public bool Add(RawEntry entry)
            {
                if (entry.Func != ComparisonFunc.EQ)
                {
                    return false;
                }

                if (this.variable == null)
                {
                    this.variable = entry.Variable;
                }
                else if (this.variable != entry.Variable)
                {
                    return false;
                }

                this.values.Add(entry.Value);
                return true;
            }

            public List<object> Get(Variable variable)
            {
                if (variable != this.variable.GetEffectiveTerm())
                {
                    return null;
                }

                return this.values;
            }
        }

        /// <summary>
        /// Terms like "variable comparison constant" or "constant comparison variable" only
        /// </summary>
        private class RawEntry
        {
            private Variable variable;
            private object value;
            private ComparisonFunc func;

            private RawEntry(Variable variable, object value, ComparisonFunc func)
            {
                this.variable = variable;
                this.value = value;
                this.func = func;
            }

            public Variable Variable
            {
                get
                {
                    return this.variable;
                }
            }

            public object Value
            {
                get
                {
                    return this.value;
                }
            }

            public ComparisonFunc Func
            {
                get
                {
                    return this.func;
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

            public void AddToConstaint(Constraint constraint, CompoundTerm term)
            {
                if (this.func == ComparisonFunc.EQ)
                {
                    constraint.AddValueEntry(new ValueEntry(term, this.variable, this.value));
                }
                else if (this.func == ComparisonFunc.LT || this.func == ComparisonFunc.LE)
                {
                    constraint.AddUpperBoundEntry(new BoundEntry(term, this.variable, this.value, this.func == ComparisonFunc.LE));
                }
                else if (this.func == ComparisonFunc.GT || this.func == ComparisonFunc.GE)
                {
                    constraint.AddLowerBoundEntry(new BoundEntry(term, this.variable, this.value, this.func == ComparisonFunc.GE));
                }
            }
        }
    }
}
