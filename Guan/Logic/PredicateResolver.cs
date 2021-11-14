//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Base class for resolving a goal.
    /// </summary>
    public abstract class PredicateResolver
    {
        private CompoundTerm input;
        private Constraint constraint;
        private QueryContext context;
        private int max;
        private int iteration;
        private bool completed;

        protected PredicateResolver(CompoundTerm input, Constraint constraint, QueryContext context, int max = int.MaxValue)
        {
            this.input = input;
            this.constraint = constraint;
            this.context = context;
            this.max = max;
            this.iteration = 0;
            this.completed = false;

            if (input != null && input.Option.Max != 0)
            {
                this.max = input.Option.Max;
            }
        }

        public CompoundTerm Input
        {
            get
            {
                return this.input;
            }

            internal set
            {
                this.input = value;
            }
        }

        public QueryContext Context
        {
            get
            {
                return this.context;
            }
        }

        public int Iteration
        {
            get
            {
                return this.iteration;
            }

            internal set
            {
                this.iteration = value;
            }
        }

        public bool Completed
        {
            get
            {
                return this.completed || this.context.IsCancelled;
            }
        }

        internal Constraint Constraint
        {
            get
            {
                return this.constraint;
            }

            set
            {
                this.constraint = value;
            }
        }

        internal int Max
        {
            get
            {
                return this.max;
            }

            set
            {
                this.max = value;
            }
        }

        public async Task<UnificationResult> GetNextAsync()
        {
            if (this.completed)
            {
                return null;
            }

            UnificationResult result;
            try
            {
                result = await this.OnGetNextAsync();
            }
            catch (Exception e)
            {
                if (!this.Input.Option.CatchException)
                {
                    throw;
                }

                EventLogWriter.WriteError("Fail\t[{0},{1}]: {2} with Exception\n{3}", this.Input.Binding.Level, this.iteration, this.Input, e);
                result = null;
            }

            if (result != null)
            {
                this.iteration++;
                if (this.iteration >= this.max)
                {
                    this.completed = true;
                }
            }
            else
            {
                this.completed = true;
            }

            return result;
        }

        public Term GetInputArgument(string name, bool includeContext = false)
        {
            Term arg = this.Input.GetArgument(name);
            if (arg == null && includeContext)
            {
                arg = this.context[name] as Term;
            }

            return arg?.GetEffectiveTerm();
        }

        public Term GetInputArgument(int index, bool optional = false)
        {
            if (index < 0 || index >= this.Input.Arguments.Count)
            {
                if (optional)
                {
                    return null;
                }

                throw new ArgumentException($"{this.input} does not have argument with index {index}");
            }

            TermArgument arg = this.Input.Arguments[index];
            return arg.Value.GetEffectiveTerm();
        }

        public object GetInputArgumentObject(string name, bool includeContext = false)
        {
            Term input = this.GetInputArgument(name);
            if (input == null && includeContext)
            {
                object result = this.context[name];
                input = result as Term;
                if (input == null)
                {
                    return result;
                }
            }

            return input?.GetObjectValue();
        }

        public object GetInputArgumentObject(int index, bool includeContext = false)
        {
            Term input = this.GetInputArgument(index, includeContext);
            return input?.GetObjectValue();
        }

        public string GetInputArgumentString(string name, bool includeContext = false)
        {
            return (string)this.GetInputArgumentObject(name, includeContext);
        }

        public string GetInputArgumentString(int index, bool includeContext = false)
        {
            return (string)this.GetInputArgumentObject(index, includeContext);
        }

        public bool GetInputArgumentFlag(string name, bool includeContext = false)
        {
            object value = this.GetInputArgumentObject(name);
            return Utility.Convert<bool>(value);
        }

        public T GetInputArgument<T>(string name)
        {
            object value = this.GetInputArgumentObject(name);
            return Utility.Convert<T>(value);
        }

        public object GetBoundValue(string name, bool remove)
        {
            List<object> result = this.GetBoundValues(name, remove);
            if (result == null)
            {
                return null;
            }

            ReleaseAssert.IsTrue(result.Count == 1);
            return result[0];
        }

        public List<object> GetBoundValues(string name, bool remove)
        {
            Term term = this.GetInputArgument(name);
            if (term == null)
            {
                return null;
            }

            if (term.IsGround())
            {
                List<object> result = new List<object>(1)
                {
                    term.GetObjectValue()
                };

                return result;
            }

            Variable variable = term as Variable;
            if (variable == null)
            {
                return null;
            }

            return this.constraint.GetValues(variable, remove);
        }

        public object GetLowerBound(string name, bool remove, out bool isInclusive)
        {
            isInclusive = false;

            Variable variable = this.GetVariable(name);
            if (variable == null)
            {
                return null;
            }

            return this.constraint.GetLowerBound(variable, remove, out isInclusive);
        }

        public object GetUpperBound(string name, bool remove, out bool isInclusive)
        {
            isInclusive = false;

            Variable variable = this.GetVariable(name);
            if (variable == null)
            {
                return null;
            }

            return this.constraint.GetUpperBound(variable, remove, out isInclusive);
        }

        public void Cancel()
        {
            if (!this.completed)
            {
                this.OnCancel();
                this.completed = true;
            }
        }

        public virtual void OnBacktrack()
        {
        }

        public abstract Task<UnificationResult> OnGetNextAsync();

        internal void WriteTrace(string type)
        {
            if (this.Input != null && !(this.Input.Functor is ConstraintPredicateType))
            {
                bool enabled;
                bool highlight;
                if (this.iteration == 0)
                {
                    (enabled, highlight) = this.Input.Option.IsTraceEnabled(type);
                }
                else
                {
                    enabled = highlight = false;
                }

                if (enabled || this.Context.EnableTrace)
                {
                    if (highlight)
                    {
                        EventLogWriter.WriteError("{0}\t[{1},{2}]: {3}", type, this.Input.Binding.Level, this.iteration, this.Input);
                    }
                    else
                    {
                        EventLogWriter.WriteInfo("{0}\t[{1},{2}]: {3}", type, this.Input.Binding.Level, this.iteration, this.Input);
                    }
                }
            }
        }

        protected virtual void OnCancel()
        {
        }

        protected void Complete()
        {
            this.completed = true;
        }

        private Variable GetVariable(string name)
        {
            Term term = this.GetInputArgument(name);
            if (term == null)
            {
                return null;
            }

            return term as Variable;
        }
    }
}
