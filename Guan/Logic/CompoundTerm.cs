//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Compound term.
    /// A grounded compound term can be used as a dictionary.
    /// </summary>
    public class CompoundTerm : Term, IPropertyContext, IWritablePropertyContext
    {
        internal const string EffectiveTypeArgumentName = "_EffectiveType";
        private static readonly char[] PropertyDelimiters = new char[] { '.', '/' };
        private static readonly List<TermArgument> EmptyArguments = new List<TermArgument>();

        private Functor functor;
        private VariableBinding binding;
        private List<TermArgument> arguments;
        private TermOption option;

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

        internal CompoundTerm(Functor functor, VariableBinding binding)
            : this(functor, binding, EmptyArguments)
        {
        }

        internal CompoundTerm(Functor functor, VariableBinding binding, List<TermArgument> arguments)
        {
            this.functor = functor;
            this.binding = binding;
            this.arguments = arguments;
            this.option = TermOption.Default;
        }

        public Functor Functor
        {
            get
            {
                return this.functor;
            }

            internal set
            {
                this.functor = value;
            }
        }

        public PredicateType PredicateType
        {
            get
            {
                return (PredicateType)this.functor;
            }
        }

        public List<TermArgument> Arguments
        {
            get
            {
                return this.arguments;
            }
        }

        public TermOption Option
        {
            get
            {
                return this.option;
            }
        }

        internal override VariableBinding Binding
        {
            get
            {
                return this.binding;
            }
        }

        public virtual object this[string name]
        {
            get
            {
                CompoundTerm current = this;
                Term term;
                int start = 0;
                while (current != null)
                {
                    int i = name.IndexOfAny(PropertyDelimiters, start);
                    if (i < 0)
                    {
                        return current.GetExtendedArgument(name.Substring(start));
                    }

                    term = current.GetExtendedArgument(name.Substring(start, i - start));
                    current = term as CompoundTerm;
                    if (current == null && term != null)
                    {
                        Constant constant = term as Constant;
                        if (constant != null)
                        {
                            current = ObjectCompundTerm.Create(constant.Value);
                        }
                    }

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
                        Term child = this.GetArgument(segment);
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
                }
                while (i > 0);

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

        public virtual IEnumerable<TermArgument> GetUnificationArgument()
        {
            return this.arguments;
        }

        public IEnumerator GetEnumerator()
        {
            return this.arguments.GetEnumerator();
        }

        public Term GetArgument(string name)
        {
            int i = this.FindArgument(name);
            if (i < 0)
            {
                return null;
            }

            return this.arguments[i].Value;
        }

        public void AddArgument(Term argument, string name, ArgumentDescription desc = null)
        {
            if (this.arguments == EmptyArguments)
            {
                this.arguments = new List<TermArgument>();
            }

            this.arguments.Add(new TermArgument(name, argument, desc));
        }

        public bool RemoveArgument(string name)
        {
            int i = this.FindArgument(name);
            if (i < 0)
            {
                return false;
            }

            this.arguments.RemoveAt(i);
            return true;
        }

        public virtual Term GetExtendedArgument(string name)
        {
            return this.GetArgument(name);
        }

        public override bool IsGround()
        {
            Stack<CompoundTerm> terms = new Stack<CompoundTerm>();
            terms.Push(this);

            while (terms.Count > 0)
            {
                CompoundTerm term = terms.Pop();
                foreach (TermArgument arg in term.Arguments)
                {
                    CompoundTerm compound = arg.Value as CompoundTerm;
                    if (compound != null)
                    {
                        terms.Push(compound);
                    }
                    else if (!arg.Value.IsGround())
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override Term GetEffectiveTerm()
        {
            EvaluatedFunctor func = this.functor as EvaluatedFunctor;
            if (func != null && func.Func is StandaloneFunc)
            {
                return func.Evaluate(this, null);
            }

            Term term = this.GetArgument("this");
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

        public override string ToString()
        {
            if (this.functor?.Name == ".")
            {
                return ListTerm.ToString(this);
            }

            StringBuilder result = new StringBuilder();
            _ = result.AppendFormat("{0}(", this.functor);

            this.OutputArguments(result);
            if (this.Arguments.Count > 0)
            {
                result.Length--;
            }

            _ = result.Append(')');
            return result.ToString();
        }

        internal CompoundTerm DuplicateInput(VariableBinding binding)
        {
            if (this.IsGround())
            {
                return this;
            }

            ReleaseAssert.IsTrue(this.binding != binding);

            CompoundTerm result = new CompoundTerm(this.Functor, binding);
            for (int i = 0; i < this.arguments.Count; i++)
            {
                Term term = this.arguments[i].Value.GetEffectiveTerm();
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

                result.AddArgument(term, this.arguments[i].Name);
            }

            return result;
        }

        internal CompoundTerm DuplicateOutput(VariableBinding binding)
        {
            if (this.IsGround())
            {
                return this;
            }

            CompoundTerm result = new CompoundTerm(this.Functor, binding);
            for (int i = 0; i < this.arguments.Count; i++)
            {
                Term term = this.arguments[i].Value.GetEffectiveTerm();
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

                result.AddArgument(term, this.arguments[i].Name);
            }

            return result;
        }

        internal CompoundTerm DuplicateGoal(VariableBinding binding)
        {
            ReleaseAssert.IsTrue(this.Binding == VariableBinding.Ground || this.IsGround());

            CompoundTerm result = new CompoundTerm(this.Functor, binding);
            result.option = this.option;

            for (int i = 0; i < this.arguments.Count; i++)
            {
                Term term = this.arguments[i].Value;
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

                result.AddArgument(term, this.arguments[i].Name);
            }

            result.option = this.option;

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
            Stack<CompoundTerm> compounds = new Stack<CompoundTerm>();
            compounds.Push(this);

            while (compounds.Count > 0)
            {
                CompoundTerm current = compounds.Pop();

                for (int i = 0; i < current.arguments.Count; i++)
                {
                    Term term = current.arguments[i].Value.GetEffectiveTerm();
                    CompoundTerm compound = term as CompoundTerm;
                    if (compound != null)
                    {
                        compounds.Push(compound);
                    }
                    else
                    {
                        OutputVariable outputVariable = term as OutputVariable;
                        if (outputVariable != null)
                        {
                            ReleaseAssert.IsTrue(outputVariable.Original.Binding == binding);
                            current.arguments[i].Value = outputVariable.Original;
                        }
                        else
                        {
                            Variable variable = term as Variable;
                            if (variable != null)
                            {
                                current.arguments[i].Value = binding.AddForeignVariable(variable);
                            }
                        }
                    }
                }

                current.binding = binding;
            }
        }

        internal EvaluatedFunctor GetEvaluatedFunctor()
        {
            EvaluatedFunctor evaluatedFunctor = this.functor as EvaluatedFunctor;
            if (evaluatedFunctor == null)
            {
                ConstraintPredicateType constraintType = this.functor as ConstraintPredicateType;
                if (constraintType != null)
                {
                    evaluatedFunctor = constraintType.Func;
                }
            }

            return evaluatedFunctor;
        }

        internal Term Evaluate(QueryContext context)
        {
            EvaluatedFunctor evaluatedFunctor = this.GetEvaluatedFunctor();
            ReleaseAssert.IsTrue(evaluatedFunctor != null, "{0} can't be evaluted", this);

            return evaluatedFunctor?.Evaluate(this, context);
        }

        internal void PostProcessing(Rule rule)
        {
            this.ProcessOption(rule);

            try
            {
                this.PredicateType.AdjustTerm(this, rule);
            }
            catch (GuanException e)
            {
                throw new GuanException(e, "Invalid {0} {1} in rule: {2}", this == rule.Head ? "head" : "goal", this, rule);
            }
        }

        internal void OutputArguments(StringBuilder result)
        {
            for (int i = 0; i < this.Arguments.Count; i++)
            {
                CompoundTerm compound = this.arguments[i].Value as CompoundTerm;
                if ((this.arguments[i].Name == i.ToString()) || (compound != null && compound.Functor.Name == this.arguments[i].Name))
                {
                    _ = result.AppendFormat("{0},", this.arguments[i].Value);
                }
                else if (!this.Arguments[i].Name.StartsWith("_"))
                {
                    _ = result.AppendFormat("{0}={1},", this.Arguments[i].Name, this.arguments[i].Value);
                }
            }
        }

        private int FindArgument(string name)
        {
            for (int i = 0; i < this.arguments.Count; i++)
            {
                if (this.arguments[i].Name == name)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ProcessOption(Rule rule)
        {
            bool traceFail = false;

            for (int i = 0; i < this.arguments.Count; i++)
            {
                CompoundTerm term = this.arguments[i].Value as CompoundTerm;
                if (term != null && term.Functor.Name == "_option")
                {
                    if (this == rule.Head)
                    {
                        throw new GuanException("_option can't be used in head");
                    }

                    this.option = new TermOption(term);
                    this.arguments.RemoveAt(i);
                }
                else if (this.arguments[i].Value.GetStringValue() == "_traceFail")
                {
                    traceFail = true;
                    this.arguments.RemoveAt(i);
                }
            }

            if (traceFail)
            {
                if (this.option == TermOption.Default)
                {
                    this.option = new TermOption();
                }

                this.option[TermOption.Trace] = "!Fail";
            }

            PredicateType predicateType = this.functor as PredicateType;
            if (predicateType != null && this.option == TermOption.Default)
            {
                this.option = predicateType.Option;
            }
        }
    }
}
