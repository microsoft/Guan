using System.Collections;
using System.Collections.Generic;
using System.Text;
using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// Compound term.
    /// A grounded compound term can be used as a dictionary.
    /// </summary>
    public class CompoundTerm : Term, IPropertyContext, IWritablePropertyContext, IEnumerable
    {
        private Functor functor_;
        public VariableBinding binding_;
        private List<TermArgument> arguments_;
        private TermOption option_;

        private static readonly List<TermArgument> EmptyArguments = new List<TermArgument>();
        private const string EffectiveTypeArgumentName = "_EffectiveType";

        public CompoundTerm() 
            : this(Functor.Empty, VariableBinding.Ground)
        {
        }

        public CompoundTerm(string name)
            : this(new Functor(name), VariableBinding.Ground)
        {
        }

        public CompoundTerm(Functor functor)
            : this(functor, VariableBinding.Ground)
        {
        }

        public CompoundTerm(Functor functor, VariableBinding binding) 
            : this(functor, binding, EmptyArguments)
        {
        }

        internal CompoundTerm(Functor functor, VariableBinding binding, List<TermArgument> arguments)
        {
            functor_ = functor;
            binding_ = binding;
            arguments_ = arguments;
            option_ = TermOption.Default;
        }

        public Functor Functor
        {
            get
            {
                return functor_;
            }
            internal set
            {
                functor_ = value;
            }
        }

        public PredicateType PredicateType
        {
            get
            {
                return (PredicateType)functor_;
            }
        }

        internal override VariableBinding Binding
        {
            get
            {
                return binding_;
            }
        }

        public List<TermArgument> Arguments
        {
            get
            {
                return arguments_;
            }
        }

        public virtual IEnumerable<TermArgument> GetUnificationArgument()
        {
            return arguments_;
        }

        public IEnumerator GetEnumerator()
        {
            return arguments_.GetEnumerator();
        }

        public virtual Term GetArgument(string name)
        {
            int i = FindArgument(name);
            if (i < 0)
            {
                return null;
            }

            return arguments_[i].Value;
        }

        public Term GetEffetiveType()
        {
            return GetArgument(EffectiveTypeArgumentName);
        }

        public void AddArgument(Term argument, string name, ArgumentDescription desc = null)
        {
            if (arguments_ == EmptyArguments)
            {
                arguments_ = new List<TermArgument>();
            }

            arguments_.Add(new TermArgument(name, argument, desc));
        }

        public bool RemoveArgument(string name)
        {
            int i = FindArgument(name);
            if (i < 0)
            {
                return false;
            }

            arguments_.RemoveAt(i);
            return true;
        }

        private int FindArgument(string name)
        {
            for (int i = 0; i < arguments_.Count; i++)
            {
                if (arguments_[i].Name == name)
                {
                    return i;
                }
            }

            return -1;
        }

        public virtual object this[string name]
        {
            get
            {
                CompoundTerm current = this;
                int start = 0;
                while (current != null)
                {
                    int i = name.IndexOf('/', start);
                    if (i < 0)
                    {
                        Term term = current.GetArgument(name.Substring(start));
                        return term?.GetValue();
                    }

                    current = current.GetArgument(name.Substring(start, i - start)) as CompoundTerm;
                    start = i + 1;
                }

                return null;
            }
            set
            {
                CompoundTerm current = this;
                int start = 0;
                int i;
                do
                {
                    i = name.IndexOf('/', start);
                    if (i > 0)
                    {
                        string segment = name.Substring(start, i - start);
                        Term child = GetArgument(segment);
                        if (child == null)
                        {
                            CompoundTerm next = new CompoundTerm();
                            current.AddArgument(next, segment);
                            current = next;
                        }
                        else
                        {
                            current = child as CompoundTerm;
                            if (current == null)
                            {
                                throw new GuanException("Invalid path {0} to set value for {1}", name, this);
                            }
                        }

                        start = i + 1;
                    }
                } while (i > 0);

                if (start > 0)
                {
                    name = name.Substring(start);
                }

                Term term = Term.FromObject(value);
                i = current.FindArgument(name);
                if (i < 0)
                {
                    current.AddArgument(term, name);
                }
                else
                {
                    current.Arguments[i].Value = term;
                }
            }
        }

        public TermOption Option
        {
            get
            {
                return option_;
            }
        }

        public void SetOption(string name, object value)
        {
            if (option_ == TermOption.Default)
            {
                option_ = new TermOption();
            }

            option_[name] = value;
        }

        public override bool IsGround()
        {
            foreach (var arg in Arguments)
            {
                if (!arg.Value.IsGround())
                {
                    return false;
                }
            }

            return true;
        }

        public override Term GetEffectiveTerm()
        {
            EvaluatedFunctor func = functor_ as EvaluatedFunctor;
            if (func != null && func.Func is StandaloneFunc)
            {
                return func.Evaluate(this, null);
            }

            Term term = GetArgument("this");
            if (term != null)
            {
                // This arg should be a variable, when the variable is bound, it
                // contains the result term.
                CompoundTerm compound = term.GetEffectiveTerm() as CompoundTerm;
                if (compound != null)
                {
                    return compound;
                }
            }

            return this;
        }

        internal CompoundTerm DuplicateInput(VariableBinding binding)
        {
            if (IsGround())
            {
                return this;
            }

            ReleaseAssert.IsTrue(binding_ != binding);

            CompoundTerm result = new CompoundTerm(Functor, binding);
            for (int i = 0; i < arguments_.Count; i++)
            {
                Term term = arguments_[i].Value.GetEffectiveTerm();
                if (!term.IsGround())
                {
                    Variable variable = term as Variable;
                    if (variable != null)
                    {
                        term = binding.AddOutputVariable(variable);
                    }
                    else
                    {
                        CompoundTerm compound = (CompoundTerm)term;
                        term = compound.DuplicateInput(binding);
                    }
                }

                result.AddArgument(term, arguments_[i].Name);
            }

            return result;
        }

        internal CompoundTerm DuplicateOutput(VariableBinding binding)
        {
            if (IsGround())
            {
                return this;
            }

            CompoundTerm result = new CompoundTerm(Functor, binding);
            for (int i = 0; i < arguments_.Count; i++)
            {
                Term term = arguments_[i].Value.GetEffectiveTerm();
                if (!term.IsGround())
                {
                    Variable variable = term as Variable;
                    if (variable != null)
                    {
                        OutputVariable outputVariable = term as OutputVariable;
                        if (outputVariable != null)
                        {
                            term = outputVariable.Original;
                        }
                        else
                        {
                            term = binding.AddForeignVariable(variable);
                        }
                    }
                    else
                    {
                        CompoundTerm compound = (CompoundTerm)term;
                        term = compound.DuplicateOutput(binding);
                    }
                }

                result.AddArgument(term, arguments_[i].Name);
            }

            return result;
        }

        internal CompoundTerm DuplicateGoal(VariableBinding binding)
        {
            ReleaseAssert.IsTrue(Binding == VariableBinding.Ground);

            CompoundTerm result = new CompoundTerm(Functor, binding);
            for (int i = 0; i < arguments_.Count; i++)
            {
                Term term = arguments_[i].Value;
                if (!term.IsGround())
                {
                    IndexedVariable variable = term as IndexedVariable;
                    if (variable != null)
                    {
                        term = binding.GetLocalVariable(variable.Index);
                    }
                    else
                    {
                        CompoundTerm compound = (CompoundTerm)term;
                        term = compound.DuplicateGoal(binding);
                    }
                }

                result.AddArgument(term, arguments_[i].Name);
            }

            result.option_ = option_;

            return result;
        }

        /// <summary>
        /// Used during tail optimization to change the binding of the input goal
        /// from the current binding of the rule to the previous one (that will be
        /// used for output eventually) as the current binding is disappearing.
        /// </summary>
        /// <param name="binding">The binding for the output.</param>
        internal void Migrate(VariableBinding binding)
        {
            for (int i = 0; i < arguments_.Count; i++)
            {
                Term term = arguments_[i].Value.GetEffectiveTerm();
                CompoundTerm compound = term as CompoundTerm;
                if (compound != null)
                {
                    compound.Migrate(binding);
                }
                else
                {
                    OutputVariable outputVariable = term as OutputVariable;
                    if (outputVariable != null)
                    {
                        ReleaseAssert.IsTrue(outputVariable.Original.Binding == binding);
                        arguments_[i].Value = outputVariable.Original;
                    }
                    else
                    {
                        Variable variable = term as Variable;
                        if (variable != null)
                        {
                            arguments_[i].Value = binding.AddForeignVariable(variable);
                        }
                    }
                }
            }

            binding_ = binding;
        }

        internal EvaluatedFunctor GetEvaluatedFunctor()
        {
            EvaluatedFunctor evaluatedFunctor = functor_ as EvaluatedFunctor;
            if (evaluatedFunctor == null)
            {
                ConstraintPredicateType constraintType = functor_ as ConstraintPredicateType;
                if (constraintType != null)
                {
                    evaluatedFunctor = constraintType.Func;
                }
            }

            return evaluatedFunctor;
        }

        internal Term Evaluate(QueryContext context)
        {
            EvaluatedFunctor evaluatedFunctor = GetEvaluatedFunctor();
            ReleaseAssert.IsTrue(evaluatedFunctor != null, "{0} can't be evaluted", this);

            return evaluatedFunctor.Evaluate(this, context);
        }

        internal void PostProcessing(Rule rule)
        {
            ProcessOption(rule);

            try
            {
                PredicateType.AdjustTerm(this, rule);
            }
            catch (GuanException e)
            {
                throw new GuanException(e, "Invalid {0} {1} in rule: {2}", this == rule.Head ? "head" : "goal", this, rule);
            }
        }

        private void ProcessOption(Rule rule)
        {
            for (int i = 0; i < arguments_.Count; i++)
            {
                CompoundTerm term = arguments_[i].Value as CompoundTerm;
                if (term != null && term.Functor.Name == "_option")
                {
                    if (this == rule.Head)
                    {
                        throw new GuanException("_option can't be used in head");
                    }

                    option_ = new TermOption(term);
                    arguments_.RemoveAt(i);
                    return;
                }
            }
        }

        internal void OutputArguments(StringBuilder result)
        {
            for (int i = 0; i < Arguments.Count; i++)
            {
                CompoundTerm compound = arguments_[i].Value as CompoundTerm;
                if ((arguments_[i].Name == i.ToString()) || (compound != null && compound.Functor.Name == arguments_[i].Name))
                {
                    result.AppendFormat("{0},", arguments_[i].Value);
                }
                else if (!Arguments[i].Name.StartsWith("_"))
                {
                    result.AppendFormat("{0}={1},", Arguments[i].Name, arguments_[i].Value);
                }
            }
        }

        public override string ToString()
        {
            if (functor_.Name == ".")
            {
                return ListTerm.ToString(this);
            }

            StringBuilder result = new StringBuilder();
            result.AppendFormat("{0}(", functor_);

            OutputArguments(result);
            if (Arguments.Count > 0)
            {
                result.Length--;
            }

            result.Append(')');
            return result.ToString();
        }
    }
}
