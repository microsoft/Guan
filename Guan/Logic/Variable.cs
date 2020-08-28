// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// Variable term at runtime which can be bound to another term, including
    /// another variable.
    /// We are not using log-based undo of the bound value during backtracking,
    /// instead, the Variable object is coupled with the corresponding VaribleBinding
    /// object to determine whether the bound value is still valid.
    /// </summary>
    public class Variable : Term
    {
        private string name_;
        private Term value_;
        private int index_;
        private int sequence_;
        private VariableBinding binding_;

        internal Variable(string name, VariableBinding binding)
        {
            name_ = name;
            binding_ = binding;
            index_ = int.MaxValue;
            sequence_ = -1;
        }

        internal Variable(Variable other, VariableBinding binding)
        {
            name_ = other.name_;
            binding_ = binding;
            index_ = other.index_;
            sequence_ = other.sequence_;
            if (other.binding_.IsValid(other.index_, other.sequence_))
            {
                value_ = other.value_;
            }
        }

        internal void Reset()
        {
            index_ = int.MaxValue;
            sequence_ = -1;
        }

        public string Name
        {
            get
            {
                return name_;
            }
        }

        internal override VariableBinding Binding
        {
            get
            {
                return binding_;
            }
        }

        public override bool IsGround()
        {
            Term value = GetBoundTerm();
            return (value != null && value.IsGround());
        }

        public override Term GetEffectiveTerm()
        {
            Term result = GetBoundTerm();
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

        internal Term GetBoundTerm()
        {
            Term result = null;

            Variable current = this;
            while (current != null && current.value_ != null && binding_.IsValid(current.index_, sequence_))
            {
                result = current.value_;
                current = result as Variable;
            }

            return result;
        }

        public void SetValue(Term value)
        {
            ReleaseAssert.IsTrue(!binding_.IsValid(index_, sequence_));
            index_ = binding_.CurrentIndex;
            sequence_ = binding_.Sequence;
            value_ = value;
        }

        internal void UpdateValueBinding(Dictionary<Variable, Variable> mapping, VariableBinding binding)
        {
            if (value_ != null)
            {
                value_ = value_.UpdateBinding(mapping, binding);
            }
        }

        internal bool Promote()
        {
            if (binding_.IsValid(index_, sequence_))
            {
                index_ = -1;
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            Term value = GetEffectiveTerm();
            if (value != this)
            {
                return value.ToString();
            }

            return "?" + name_;
        }
    }
}
