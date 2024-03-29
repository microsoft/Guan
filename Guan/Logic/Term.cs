﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Base class for terms, including Constant, Variable and CompoundTerm.
    /// </summary>
    public abstract class Term
    {
        protected Term()
        {
        }

        internal virtual VariableBinding Binding
        {
            get
            {
                return VariableBinding.Ground;
            }
        }

        public abstract bool IsGround();

        public virtual Term GetEffectiveTerm()
        {
            return this;
        }

#pragma warning disable CA1024 // Use properties where appropriate
        public object GetObjectValue()
#pragma warning restore CA1024 // Use properties where appropriate
        {
            Constant constant = this as Constant;
            if (constant != null)
            {
                return constant.Value;
            }

            ObjectCompundTerm objectCompundTerm = this as ObjectCompundTerm;
            if (objectCompundTerm != null)
            {
                return objectCompundTerm.Value;
            }

            return null;
        }

#pragma warning disable CA1024 // Use properties where appropriate
        public string GetStringValue()
#pragma warning restore CA1024 // Use properties where appropriate
        {
            Constant constant = this as Constant;
            if (constant == null)
            {
                return null;
            }

            return constant.Value as string;
        }

        internal static Term FromObject(object value)
        {
            Term term = value as Term;
            if (term != null)
            {
                return term;
            }

            return new Constant(value);
        }

        internal CompoundTerm ToCompound()
        {
            Term term = this.GetEffectiveTerm();

            CompoundTerm result = term as CompoundTerm;
            if (result == null)
            {
                string name = term.GetStringValue();
                if (name != null)
                {
                    result = new CompoundTerm(name);
                }
            }

            return result;
        }

        internal Term ForceEvaluate(QueryContext context)
        {
            Term result = this.GetEffectiveTerm();
            ReleaseAssert.IsTrue(!(result is Variable), "{0} is not gounded", this);

            CompoundTerm compound = result as CompoundTerm;
            if (compound != null && compound.Functor is EvaluatedFunctor)
            {
                foreach (TermArgument arg in compound.Arguments)
                {
                    arg.Value = arg.Value.ForceEvaluate(context);
                }

                return compound.Evaluate(context);
            }

            return result;
        }

        /// <summary>
        /// Get a copy that gets rid of variables so that the value is
        /// not affected by backtracking,
        /// </summary>
        /// <returns>Grounded copy of the term</returns>
        internal Term GetGroundedCopy()
        {
            Term term = this.GetEffectiveTerm();

            Constant constant = term as Constant;
            if (constant != null)
            {
                return constant;
            }

            ReleaseAssert.IsTrue(!(term is Variable), "{0} is not gounded", this);

            CompoundTerm compound = (CompoundTerm)term;
            CompoundTerm result = new CompoundTerm(compound.Functor, null);
            foreach (TermArgument arg in compound.Arguments)
            {
                result.AddArgument(arg.Value.GetGroundedCopy(), arg.Name);
            }

            return result;
        }

        internal Term UpdateBinding(Dictionary<Variable, Variable> mapping, VariableBinding binding)
        {
            Term term = this.GetEffectiveTerm();

            Constant constant = term as Constant;
            if (constant != null)
            {
                return constant;
            }

            Variable variable = term as Variable;
            if (variable != null)
            {
                Variable mappedVariable;
                if (mapping.TryGetValue(variable, out mappedVariable))
                {
                    return mappedVariable;
                }

                return variable;
            }

            CompoundTerm compound = (CompoundTerm)term;
            CompoundTerm result = null;
            for (int i = 0; i < compound.Arguments.Count; i++)
            {
                Term newArg = compound.Arguments[i].Value.UpdateBinding(mapping, binding);
                if (result == null && newArg != compound.Arguments[i].Value)
                {
                    result = new CompoundTerm(compound.Functor, this.Binding);
                    for (int j = 0; j < i; j++)
                    {
                        result.AddArgument(compound.Arguments[j].Value, compound.Arguments[j].Name);
                    }
                }

                if (result != null)
                {
                    result.AddArgument(newArg, compound.Arguments[i].Name);
                }
            }

            if (result != null)
            {
                return result;
            }

            return this;
        }

        internal bool Unify(Term other)
        {
            VariableBinding binding = this.Binding;
            if (binding == VariableBinding.Ground)
            {
                binding = other.Binding;
            }

            return binding.Unify(this, other);
        }
    }
}
