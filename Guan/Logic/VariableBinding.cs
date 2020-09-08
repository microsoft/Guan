using System;
using System.Threading;
using System.Collections.Generic;
using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// The collection of variables for a given rule at runtime.
    /// </summary>
    public class VariableBinding
    {
        /// <summary>
        /// Local variables as defined in the rule.
        /// </summary>
        private Variable[] local_;

        /// <summary>
        /// Variables returned to the caller.
        /// </summary>
        private List<OutputVariable> output_;

        /// <summary>
        /// Variables returned from the goals.
        /// </summary>
        private List<LinkedVariable> foreign_;

        /// <summary>
        /// The start offset of foreign variable for each goal.
        /// </summary>
        private int[] offsets_;

        private int currentIndex_;
        private int[] sequences_;
        private int count_;
        private int level_;
        private long seq_;

        private static long Seq = 0;

        public static readonly VariableBinding Ground = new VariableBinding(new VariableTable(), 0, 0);

        public VariableBinding(VariableTable variableTable, int goalCount, int level)
        {
            level_ = level;
            seq_ = Interlocked.Increment(ref Seq);

            local_ = new Variable[variableTable.Count];
            for (int i = 0; i < variableTable.Count; i++)
            {
                local_[i] = new Variable(variableTable[i], this);
            }

            output_ = new List<OutputVariable>();

            count_ = 0;
            foreign_ = new List<LinkedVariable>(goalCount);
            offsets_ = new int[goalCount];
            sequences_ = new int[goalCount];
            for (int i = 0; i < goalCount; i++)
            {
                offsets_[i] = 0;
                sequences_[i] = 0;
            }

            currentIndex_ = -1;
        }

        public VariableBinding(VariableBinding other)
        {
            seq_ = Interlocked.Increment(ref Seq);
            level_ = other.level_;
            currentIndex_ = other.currentIndex_;
            count_ = other.count_;

            Dictionary<Variable, Variable> mapping = new Dictionary<Variable, Variable>();

            local_ = new Variable[other.local_.Length];
            for (int i = 0; i < local_.Length; i++)
            {
                local_[i] = new Variable(other.local_[i], this);
                mapping.Add(other.local_[i], local_[i]);
            }

            output_ = new List<OutputVariable>(other.output_.Count);
            foreach (OutputVariable outputVariable in other.output_)
            {
                output_.Add(new OutputVariable(outputVariable, this));
                mapping.Add(outputVariable, output_[output_.Count - 1]);
            }

            foreign_ = new List<LinkedVariable>(other.count_);
            for (int i = 0; i < other.count_; i++)
            {
                foreign_.Add(new LinkedVariable(other.foreign_[i], this));
                mapping.Add(other.foreign_[i], foreign_[foreign_.Count - 1]);
            }

            offsets_ = new int[other.offsets_.Length];
            sequences_ = new int[other.sequences_.Length];
            for (int i = 0; i < other.offsets_.Length; i++)
            {
                offsets_[i] = other.offsets_[i];
                sequences_[i] = other.sequences_[i];
            }

            foreach (Variable variable in local_)
            {
                variable.UpdateValueBinding(mapping, other);
            }
            foreach (Variable variable in output_)
            {
                variable.UpdateValueBinding(mapping, other);
            }
            foreach (Variable variable in foreign_)
            {
                variable.UpdateValueBinding(mapping, other);
            }
        }

        internal int Level
        {
            get
            {
                return level_;
            }
        }

        internal int CurrentIndex
        {
            get
            {
                return currentIndex_;
            }
        }

        internal int Sequence
        {
            get
            {
                return (currentIndex_ < 0 ? 0 : sequences_[currentIndex_]);
            }
        }

        internal bool IsValid(int index, int sequence)
        {
            return (index < 0 || (index <= currentIndex_ && sequences_[index] == sequence));
        }

        public Variable GetLocalVariable(int index)
        {
            return local_[index];
        }

        public OutputVariable AddOutputVariable(Variable original)
        {
            ReleaseAssert.IsTrue(currentIndex_ == -1);

            foreach (OutputVariable existing in output_)
            {
                if (existing.Original == original)
                {
                    return existing;
                }
            }

            OutputVariable result = new OutputVariable(this, original, "$" + output_.Count.ToString());
            output_.Add(result);

            return result;
        }

        public LinkedVariable AddForeignVariable(Variable original)
        {
            LinkedVariable result;
            for (int i = offsets_[currentIndex_]; i < count_; i++)
            {
                if (foreign_[i].Original == original)
                {
                    return foreign_[i];
                }
            }

            if (count_ < foreign_.Count)
            {
                result = foreign_[count_];
                result.Original = original;
                result.Reset();
            }
            else
            {
                result = new LinkedVariable(this, original, foreign_.Count.ToString() + "$");
                foreign_.Add(result);
            }

            count_++;
            return result;
        }

        public void MoveNext()
        {
            currentIndex_++;
            if (currentIndex_ < offsets_.Length)
            {
                offsets_[currentIndex_] = count_;
                sequences_[currentIndex_]++;
            }
        }

        public bool MovePrev()
        {
            if (currentIndex_ == 0)
            {
                return false;
            }

            if (currentIndex_ < offsets_.Length)
            {
                count_ = offsets_[currentIndex_];
            }

            currentIndex_--;
            sequences_[currentIndex_]++;

            return true;
        }

        public UnificationResult CreateOutput()
        {
            if (output_.Count == 0)
            {
                return UnificationResult.Empty;
            }

            UnificationResult result = new UnificationResult(output_.Count);
            foreach (OutputVariable entry in output_)
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
                term2 = AddOutputVariable(var2).GetEffectiveTerm();
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
            if (constant1 != null)
            {
                // If both are constants, unify if the two are equal.
                if (constant2 != null)
                {
                    return object.Equals(constant1.Value, constant2.Value);
                }

                // Try to unify as compound
                ObjectCompundTerm objectCompundTerm = ObjectCompundTerm.Create(constant1.Value);
                if (objectCompundTerm == null)
                {
                    return false;
                }

                compound2 = (CompoundTerm)term2;
                if (compound2.Functor != Functor.ClassObject)
                {
                    // Check whether class name (including base classes) matches input compound functor name
                    Type type = objectCompundTerm.ObjectType;
                    bool found = false;
                    while (!found)
                    {
                        if (type.Name == compound2.Functor.Name || type.FullName == compound2.Functor.Name)
                        {
                            found = true;
                        }
                        else
                        {
                            type = type.BaseType;
                            if (type == null)
                            {
                                return false;
                            }
                        }
                    }
                }

                compound1 = objectCompundTerm;
            }
            else
            {
                if (constant2 != null)
                {
                    return false;
                }

                compound1 = (CompoundTerm)term1;
                compound2 = (CompoundTerm)term2;
                if (!compound1.Functor.Unify(compound2.Functor))
                {
                    return false;
                }

                // If local contains effective type argument, unify it with the input functor name.
                Term arg1 = compound1.GetEffetiveType();
                if (arg1 != null && compound2.GetEffetiveType() == null && !Unify(arg1, new Constant(compound2.Functor.Name)))
                {
                    return false;
                }

                // If local contains "this" argument, unify it with the entire input term.
                arg1 = compound1.GetArgument("this");
                if (arg1 != null && compound2.IsGround() && !Unify(arg1, compound2))
                {
                    return false;
                }
            }

            // Unify every argument of input
            foreach (var arg2 in compound2.GetUnificationArgument())
            {
                // Only unify when an argument is present on both sides
                Term arg1 = (arg2.Name == "this" ? compound1 : compound1.GetArgument(arg2.Name));
                if (arg1 != null && !Unify(arg1, arg2.Value))
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
            foreach (var variable in local_)
            {
                variable.Promote();
            }

            foreach (var variable in output_)
            {
                variable.Promote();
            }

            // Retain the foreign variables that have not been instantiated. For those that
            // have already been instantiated, we don't need to keep track of them explicitly
            // in the binding: C# memory management can automatically takes care of them.
            List<LinkedVariable> foreign = new List<LinkedVariable>(foreign_.Count);
            for (int i = 0; i < count_; i++)
            {
                if (!foreign_[i].Promote())
                {
                    foreign.Add(foreign_[i]);
                }
            }

            foreign_ = foreign;
            count_ = foreign_.Count;
            currentIndex_ = 0;
        }

        public void ResetOutput()
        {
            output_.Clear();
        }

        public void ResetLast()
        {
            if (currentIndex_ >= 0)
            {
                sequences_[currentIndex_]++;
            }
        }

        public override string ToString()
        {
            return seq_.ToString();
        }
    }
}
