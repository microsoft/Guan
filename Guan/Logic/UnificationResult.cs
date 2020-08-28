using System.Collections.Generic;
using System.Text;
using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// The output from resolving a goal, including the instantiation of
    /// variables and the constraints to be propagated to the remaining
    /// goals.
    /// </summary>
    public class UnificationResult
    {
        private List<OutputVariable> entries_;
        private List<CompoundTerm> constraints_;

        internal static readonly UnificationResult Empty = new UnificationResult(0);

        public UnificationResult(int capacity)
        {
            entries_ = new List<OutputVariable>(capacity);
        }

        internal void Add(OutputVariable entry)
        {
            entries_.Add(entry);
        }

        internal bool IsEmpty
        {
            get
            {
                return (entries_.Count == 0);
            }
        }

        public List<CompoundTerm> Constraints
        {
            get
            {
                return constraints_;
            }
        }

        public void AddConstraint(CompoundTerm constraint)
        {
            if (constraints_ == null)
            {
                constraints_ = new List<CompoundTerm>();
            }

            constraints_.Add(constraint);
        }

        internal void Apply(VariableBinding binding)
        {
            ReleaseAssert.IsTrue(binding.CurrentIndex >= 0);

            foreach (var entry in entries_)
            {
                Term value = entry.GetEffectiveTerm();
                if (!value.IsGround())
                {
                    CompoundTerm compound = value as CompoundTerm;
                    if (compound != null)
                    {
                        value = compound.DuplicateOutput(binding);
                    }
                    else
                    {
                        OutputVariable outputVariable = (OutputVariable) value;
                        value = outputVariable.Original;
                    }
                }

                ReleaseAssert.IsTrue(entry.Original.Binding == binding);
                entry.Original.SetValue(value);
            }
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            foreach (var entry in entries_)
            {
                Term value = entry.GetBoundTerm();
                result.AppendFormat("?{0}={1},", entry.Original.Name, value);
            }

            if (result.Length > 1)
            {
                result.Length--;
            }

            return result.ToString();
        }
    }
}
