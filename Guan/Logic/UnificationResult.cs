// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// The output from resolving a goal, including the instantiation of
    /// variables and the constraints to be propagated to the remaining
    /// goals.
    /// </summary>
    public class UnificationResult
    {
        internal static readonly UnificationResult Empty = new UnificationResult(0);

        private List<OutputVariable> entries;
        private List<CompoundTerm> constraints;

        public UnificationResult(int capacity)
        {
            this.entries = new List<OutputVariable>(capacity);
        }

        internal List<CompoundTerm> Constraints
        {
            get
            {
                return this.constraints;
            }
        }

        internal bool IsEmpty
        {
            get
            {
                return (this.entries.Count == 0);
            }
        }

        internal List<OutputVariable> Entries
        {
            get
            {
                return this.entries;
            }
        }

        public Term this[string name]
        {
            get
            {
                foreach (OutputVariable output in this.entries)
                {
                    if (output.Original.Name == name)
                    {
                        return output.GetEffectiveTerm();
                    }
                }

                return null;
            }
        }

        public void AddConstraint(CompoundTerm constraint)
        {
            if (this.constraints == null)
            {
                this.constraints = new List<CompoundTerm>();
            }

            this.constraints.Add(constraint);
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            foreach (OutputVariable entry in this.entries)
            {
                if (!entry.Original.Name.StartsWith("_"))
                {
                    Term value = entry.GetBoundTerm();
                    LinkedVariable linkedVariable = value as LinkedVariable;
                    if (linkedVariable != null)
                    {
                        _ = result.AppendFormat("?{0}=?{1},", entry.Original.Name, linkedVariable.Original.Name);
                    }
                    else
                    {
                        Constant constant = value as Constant;
                        if (constant != null)
                        {
                            value = ObjectCompundTerm.Create(constant.Value);
                            if (value == null)
                            {
                                value = constant;
                            }
                        }

                        _ = result.AppendFormat("?{0}={1},", entry.Original.Name, value);
                    }
                }
            }

            if (result.Length > 1)
            {
                result.Length--;
            }

            return result.ToString();
        }

        internal void Add(OutputVariable entry)
        {
            this.entries.Add(entry);
        }

        internal void Apply(VariableBinding binding)
        {
            ReleaseAssert.IsTrue(binding.CurrentIndex >= 0);

            foreach (OutputVariable entry in this.entries)
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
                        OutputVariable outputVariable = (OutputVariable)value;
                        value = outputVariable.Original;
                    }
                }

                ReleaseAssert.IsTrue(entry.Original.Binding == binding);
                entry.Original.SetValue(value);
            }
        }
    }
}
