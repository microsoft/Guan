// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Variable term at runtime which can be bound to another term, including
    /// another variable.
    /// We are not using log-based undo of the bound value during backtracking,
    /// instead, the Variable object is coupled with the corresponding VaribleBinding
    /// object to determine whether the bound value is still valid.
    /// </summary>
    public class Variable : Term
    {
        private string name;
        private Term value;
        private int index;
        private int sequence;
        private VariableBinding binding;

        internal Variable(string name, VariableBinding binding)
        {
            this.name = name;
            this.binding = binding;
            this.index = int.MaxValue;
            this.sequence = -1;
        }

        internal Variable(Variable other, VariableBinding binding)
        {
            this.name = other.name;
            this.binding = binding;
            this.index = other.index;
            this.sequence = other.sequence;
            if (other.binding.IsValid(other.index, other.sequence))
            {
                this.value = other.value;
            }
        }

        public string Name
        {
            get
            {
                return this.name;
            }
        }

        internal override VariableBinding Binding
        {
            get
            {
                return this.binding;
            }
        }

        public override bool IsGround()
        {
            Term value = this.GetBoundTerm();
            return (value != null && value.IsGround());
        }

        public override Term GetEffectiveTerm()
        {
            Term result = this.GetBoundTerm();
            if (result == null)
            {
                result = this;
            }
            else
            {
                result = result.GetEffectiveTerm();
            }

            return result;
        }

        public void SetValue(Term value)
        {
            ReleaseAssert.IsTrue(!this.binding.IsValid(this.index, this.sequence));
            this.index = this.binding.CurrentIndex;
            this.sequence = this.binding.Sequence;
            this.value = value;
        }

        public override string ToString()
        {
            Term value = this.GetEffectiveTerm();
            if (value != this)
            {
                return value.ToString();
            }

            return "?" + this.name;
        }

        internal void Reset()
        {
            this.index = int.MaxValue;
            this.sequence = -1;
        }

        internal Term GetBoundTerm()
        {
            Term result = null;

            Variable current = this;
            while (current != null && current.value != null && this.binding.IsValid(current.index, this.sequence))
            {
                result = current.value;
                current = result as Variable;
            }

            return result;
        }

        internal void UpdateValueBinding(Dictionary<Variable, Variable> mapping, VariableBinding binding)
        {
            if (this.value != null)
            {
                this.value = this.value.UpdateBinding(mapping, binding);
            }
        }

        internal bool Promote()
        {
            if (this.binding.IsValid(this.index, this.sequence))
            {
                this.index = -1;
                return true;
            }

            return false;
        }
    }
}
