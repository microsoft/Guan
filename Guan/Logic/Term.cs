using System.Collections.Generic;
using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// Base class for terms, including Constant, Variable and CompoundTerm.
    /// </summary>
    public abstract class Term
    {
        public Term()
        {
        }

        public virtual VariableBinding Binding
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

        public object GetValue()
        {
            ReleaseAssert.IsTrue(IsGround());

            Constant constant = this as Constant;

            if (constant != null)
            {
                return constant.Value;
            }

            return this;
        }

        public string GetStringValue()
        {
            Constant constant = this as Constant;

            if (constant == null)
            {
                return null;
            }

            return constant.Value as string;
        }

        public Term ForceEvaluate(QueryContext context)
        {
            Term result = GetEffectiveTerm();
            ReleaseAssert.IsTrue(!(result is Variable), "{0} is not gounded", this);

            CompoundTerm compound = result as CompoundTerm;
            if (compound != null && compound.Functor is EvaluatedFunctor)
            {
                foreach (var arg in compound.Arguments)
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
        public Term GetGroundedCopy()
        {
            Term term = GetEffectiveTerm();

            Constant constant = term as Constant;

            if (constant != null)
            {
                return constant;
            }

            ReleaseAssert.IsTrue(!(term is Variable), "{0} is not gounded", this);

            CompoundTerm compound = (CompoundTerm)term;
            CompoundTerm result = new CompoundTerm(compound.Functor, null);
            foreach (var arg in compound.Arguments)
            {
                result.AddArgument(arg.Value.GetGroundedCopy(), arg.Name);
            }

            return result;
        }

        public Term UpdateBinding(Dictionary<Variable, Variable> mapping, VariableBinding binding)
        {
            Term term = GetEffectiveTerm();

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
                    result = new CompoundTerm(compound.Functor, Binding);
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

        public bool Unify(Term other)
        {
            VariableBinding binding = Binding;
            if (binding == VariableBinding.Ground)
            {
                binding = other.Binding;
            }

            return binding.Unify(this, other);
        }

        public static Term FromObject(object value)
        {
            Term term = value as Term;
            if (term != null)
            {
                return term;
            }

            return new Constant(value);
        }
    }
}
