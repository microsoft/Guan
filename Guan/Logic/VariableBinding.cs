// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// The collection of variables for a given rule at runtime.
    /// </summary>
    internal class VariableBinding
    {
        internal static readonly VariableBinding Ground = new VariableBinding(new VariableTable(), 0, 0);

        private static long seqGenerator = 0;

        /// <summary>
        /// Local variables as defined in the rule.
        /// </summary>
        private Variable[] local;

        /// <summary>
        /// Variables returned to the caller.
        /// </summary>
        private List<OutputVariable> output;

        /// <summary>
        /// Variables returned from the goals.
        /// </summary>
        private List<LinkedVariable> foreign;

        /// <summary>
        /// The start offset of foreign variable for each goal.
        /// </summary>
        private int[] offsets;

        private int currentIndex;
        private int[] sequences;
        private int count;
        private int level;
        private long seq;

        public VariableBinding(VariableTable variableTable, int goalCount, int level)
        {
            this.level = level;
            this.seq = Interlocked.Increment(ref seqGenerator);

            this.local = new Variable[variableTable.Count];
            for (int i = 0; i < variableTable.Count; i++)
            {
                this.local[i] = new Variable(variableTable[i], this);
            }

            this.output = new List<OutputVariable>();

            this.count = 0;
            this.foreign = new List<LinkedVariable>(goalCount);
            this.offsets = new int[goalCount];
            this.sequences = new int[goalCount];
            for (int i = 0; i < goalCount; i++)
            {
                this.offsets[i] = 0;
                this.sequences[i] = 0;
            }

            this.currentIndex = -1;
        }

        public VariableBinding(VariableBinding other)
        {
            this.seq = Interlocked.Increment(ref seqGenerator);
            this.level = other.level;
            this.currentIndex = other.currentIndex;
            this.count = other.count;

            Dictionary<Variable, Variable> mapping = new Dictionary<Variable, Variable>();

            this.local = new Variable[other.local.Length];
            for (int i = 0; i < this.local.Length; i++)
            {
                this.local[i] = new Variable(other.local[i], this);
                mapping.Add(other.local[i], this.local[i]);
            }

            this.output = new List<OutputVariable>(other.output.Count);
            foreach (OutputVariable outputVariable in other.output)
            {
                this.output.Add(new OutputVariable(outputVariable, this));
                mapping.Add(outputVariable, this.output[this.output.Count - 1]);
            }

            this.foreign = new List<LinkedVariable>(other.count);
            for (int i = 0; i < other.count; i++)
            {
                this.foreign.Add(new LinkedVariable(other.foreign[i], this));
                mapping.Add(other.foreign[i], this.foreign[this.foreign.Count - 1]);
            }

            this.offsets = new int[other.offsets.Length];
            this.sequences = new int[other.sequences.Length];
            for (int i = 0; i < other.offsets.Length; i++)
            {
                this.offsets[i] = other.offsets[i];
                this.sequences[i] = other.sequences[i];
            }

            foreach (Variable variable in this.local)
            {
                variable.UpdateValueBinding(mapping, other);
            }

            foreach (Variable variable in this.output)
            {
                variable.UpdateValueBinding(mapping, other);
            }

            foreach (Variable variable in this.foreign)
            {
                variable.UpdateValueBinding(mapping, other);
            }
        }

        internal int Level
        {
            get
            {
                return this.level;
            }
        }

        internal int CurrentIndex
        {
            get
            {
                return this.currentIndex;
            }
        }

        internal int Sequence
        {
            get
            {
                return (this.currentIndex < 0 ? 0 : this.sequences[this.currentIndex]);
            }
        }

        public Variable GetLocalVariable(int index)
        {
            return this.local[index];
        }

        public OutputVariable AddOutputVariable(Variable original)
        {
            ReleaseAssert.IsTrue(this.currentIndex == -1);

            foreach (OutputVariable existing in this.output)
            {
                if (existing.Original == original)
                {
                    return existing;
                }
            }

            OutputVariable result = new OutputVariable(this, original, "$" + this.output.Count.ToString());
            this.output.Add(result);

            return result;
        }

        public LinkedVariable AddForeignVariable(Variable original)
        {
            LinkedVariable result;
            for (int i = this.offsets[this.currentIndex]; i < this.count; i++)
            {
                if (this.foreign[i].Original == original)
                {
                    return this.foreign[i];
                }
            }

            if (this.count < this.foreign.Count)
            {
                result = this.foreign[this.count];
                result.Original = original;
                result.Reset();
            }
            else
            {
                result = new LinkedVariable(this, original, this.foreign.Count.ToString() + "$");
                this.foreign.Add(result);
            }

            this.count++;
            return result;
        }

        public void MoveNext()
        {
            this.currentIndex++;
            if (this.currentIndex < this.offsets.Length)
            {
                this.offsets[this.currentIndex] = this.count;
                this.sequences[this.currentIndex]++;
            }
        }

        public bool MovePrev()
        {
            if (this.currentIndex == 0)
            {
                return false;
            }

            if (this.currentIndex < this.offsets.Length)
            {
                this.count = this.offsets[this.currentIndex];
            }

            this.currentIndex--;
            this.sequences[this.currentIndex]++;

            return true;
        }

        public UnificationResult CreateOutput()
        {
            if (this.output.Count == 0)
            {
                return UnificationResult.Empty;
            }

            UnificationResult result = new UnificationResult(this.output.Count);
            foreach (OutputVariable entry in this.output)
            {
                if (entry.GetBoundTerm() != null)
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        public bool Unify(Term term1, Term term2)
        {
            term1 = term1.GetEffectiveTerm();
            Variable var1 = term1 as Variable;

            term2 = term2.GetEffectiveTerm();
            Variable var2 = term2 as Variable;
            if (var2 != null && var2.Binding != this)
            {
                // The output variable might already be bound, so refreshing the values
                term2 = this.AddOutputVariable(var2).GetEffectiveTerm();
                var2 = term2 as Variable;
            }

            if (term1 == term2)
            {
                return true;
            }

            if (var2 != null)
            {
                // If both are variables, bind input to local, or both are local binding
                // anyone to the other is fine.
                if (var1 != null && var2 is LinkedVariable)
                {
                    var1.SetValue(term2);
                }
                else
                {
                    var2.SetValue(term1);
                }

                return true;
            }

            CompoundTerm compound1;
            CompoundTerm compound2;
            if (var1 != null)
            {
                compound2 = term2 as CompoundTerm;
                // If local is variable and the input is compound, we need to bind
                // the local variable to the compound mapped to local binding.
                if (compound2 != null && compound2.Binding != this)
                {
                    term2 = compound2.DuplicateInput(this);
                }

                ReleaseAssert.IsTrue(var1 != term2);
                var1.SetValue(term2);
                return true;
            }

            // At this point neither is variable.
            // In addition to unify constant/constant and compound/compound, we also try
            // to unify constant(object)/compound, which can be useful for external predicates.
            // compound/constant is not currently being unified until we find a use case for it.
            Constant constant1 = term1 as Constant;
            Constant constant2 = term2 as Constant;
            if (constant2 != null)
            {
                return (constant1 != null && object.Equals(constant1.Value, constant2.Value));
            }

            compound2 = (CompoundTerm)term2;
            if (constant1 != null)
            {
                compound1 = ObjectCompundTerm.Create(constant1.Value);
                if (compound1 == null)
                {
                    return false;
                }
            }
            else
            {
                compound1 = (CompoundTerm)term1;
            }

            if (!UnifyFunctor(compound1, compound2))
            {
                return false;
            }

            // Unify every argument of input
            foreach (TermArgument arg2 in compound2.GetUnificationArgument())
            {
                // Only unify when an argument is present on both sides
                Term arg1;
                if (arg2.Name == "this")
                {
                    arg1 = term1;
                }
                else
                {
                    arg1 = compound1[arg2.Name] as Term;
                }

                if (arg1 != null && !this.Unify(arg1, arg2.Value))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Update the binding during tail optimization.
        /// </summary>
        public void ResetTail()
        {
            // These variables won't backtrack so assign -1 as their index.
            foreach (Variable variable in this.local)
            {
                _ = variable.Promote();
            }

            foreach (OutputVariable variable in this.output)
            {
                _ = variable.Promote();
            }

            // Retain the foreign variables that have not been instantiated. For those that
            // have already been instantiated, we don't need to keep track of them explicitly
            // in the binding: C# memory management can automatically takes care of them.
            List<LinkedVariable> foreign = new List<LinkedVariable>(this.foreign.Count);
            for (int i = 0; i < this.count; i++)
            {
                if (!this.foreign[i].Promote())
                {
                    foreign.Add(this.foreign[i]);
                }
            }

            this.foreign = foreign;
            this.count = this.foreign.Count;
            this.currentIndex = 0;
        }

        public void ResetOutput()
        {
            this.output.Clear();
        }

        public void ResetLast()
        {
            if (this.currentIndex >= 0)
            {
                this.sequences[this.currentIndex]++;
            }
        }

        public bool SetLocalVariableValue(string name, Term value)
        {
            for (int i = 0; i < this.local.Length; i++)
            {
                if (this.local[i].Name == name)
                {
                    this.local[i].SetValue(value);
                }
            }

            return false;
        }

        public override string ToString()
        {
            return this.seq.ToString();
        }

        internal bool IsValid(int index, int sequence)
        {
            return (index < 0 || (index <= this.currentIndex && this.sequences[index] == sequence));
        }

        private static bool UnifyFunctor(CompoundTerm term1, CompoundTerm term2)
        {
            (string name1, Type type1) = GetFunctorType(term1);
            (string name2, Type type2) = GetFunctorType(term2);
            if (type1 == null && type2 == null)
            {
                return (name1 == name2);
            }
            else if (type1 != null && type2 != null)
            {
                return type1.IsAssignableFrom(type2) || type2.IsAssignableFrom(type1);
            }
            else if (type1 == null)
            {
                type1 = type2;
                name2 = name1;
            }

            if (type1 == typeof(ObjectCompundTerm) || name2 == Functor.ClassObject.Name)
            {
                return true;
            }

            while (type1 != null)
            {
                if (type1.Name == name2 || type1.FullName == name2)
                {
                    return true;
                }

                type1 = type1.BaseType;
            }

            return false;
        }

        private static (string, Type) GetFunctorType(CompoundTerm term)
        {
            Term arg = term.GetArgument(CompoundTerm.EffectiveTypeArgumentName);
            if (arg != null)
            {
                return (arg.GetStringValue(), null);
            }

            return (term.Functor.Name, term.Functor.UnificationType);
        }
    }
}
