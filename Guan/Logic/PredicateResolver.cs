// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// Base class for resolving a goal.
    /// </summary>
    public abstract class PredicateResolver
    {
        private CompoundTerm input_;
        private Constraint constraint_;
        private QueryContext context_;
        private int max_;
        private int iteration_;
        private bool completed_;

        private static bool EnableTrace = Utility.GetConfig("EnableTrace", false);

        public PredicateResolver(CompoundTerm input, Constraint constraint, QueryContext context, int max = int.MaxValue)
        {
            input_ = input;
            constraint_ = constraint;
            context_ = context;
            max_ = max;
            iteration_ = 0;
            completed_ = false;

            if (input != null && input.Option.Max != 0)
            {
                max_ = input.Option.Max;
            }
        }

        public CompoundTerm Input
        {
            get
            {
                return input_;
            }
            internal set
            {
                input_ = value;
            }
        }

        internal Constraint Constraint
        {
            get
            {
                return constraint_;
            }
            set
            {
                constraint_ = value;
            }
        }

        public QueryContext Context
        {
            get
            {
                return context_;
            }
        }

        public int Iteration
        {
            get
            {
                return iteration_;
            }
            internal set
            {
                iteration_ = value;
            }
        }

        internal int Max
        {
            get
            {
                return max_;
            }
            set
            {
                max_ = value;
            }
        }

        public bool Completed
        {
            get
            {
                return completed_ || context_.IsCancelled;
            }
        }

        protected void Complete()
        {
            completed_ = true;
        }

        public async Task<UnificationResult> GetNextAsync()
        {
            if (completed_)
            {
                return null;
            }

            UnificationResult result = await OnGetNextAsync();
            if (result != null)
            {
                iteration_++;
                if (iteration_ >= max_)
                {
                    completed_ = true;
                }
            }
            else
            {
                completed_ = true;
            }

            return result;
        }

        public object GetBoundValue(string name, bool remove)
        {
            List<object> result = GetBoundValues(name, remove);
            if (result == null)
            {
                return null;
            }

            ReleaseAssert.IsTrue(result.Count == 1);
            return result[0];
        }

        public List<object> GetBoundValues(string name, bool remove)
        {
            Term term = GetTerm(name);
            if (term == null)
            {
                return null;
            }

            if (term.IsGround())
            {
                List<object> result = new List<object>(1)
                {
                    term.GetValue()
                };
                return result;
            }

            Variable variable = term as Variable;
            if (variable == null)
            {
                return null;
            }

            return constraint_.GetValues(variable, remove);
        }

        public object GetLowerBound(string name, bool remove, out bool isInclusive)
        {
            isInclusive = false;

            Variable variable = GetVariable(name);
            if (variable == null)
            {
                return null;
            }

            return constraint_.GetLowerBound(variable, remove, out isInclusive);
        }

        public object GetUpperBound(string name, bool remove, out bool isInclusive)
        {
            isInclusive = false;

            Variable variable = GetVariable(name);
            if (variable == null)
            {
                return null;
            }

            return constraint_.GetUpperBound(variable, remove, out isInclusive);
        }

        private Term GetTerm(string name)
        {
            Term arg = Input.GetArgument(name);
            if (arg == null)
            {
                return null;
            }

            return arg.GetEffectiveTerm();
        }

        private Variable GetVariable(string name)
        {
            Term term = GetTerm(name);
            if (term == null)
            {
                return null;
            }

            return term as Variable;
        }

        public void Cancel()
        {
            if (!completed_)
            {
                OnCancel();
                completed_ = true;
            }
        }

        protected virtual void OnCancel()
        {
        }

        public virtual void OnBacktrack()
        {
        }

        public abstract Task<UnificationResult> OnGetNextAsync();

        internal void WriteTrace(string type)
        {
            if (Input != null && !(Input.Functor is ConstraintPredicateType) && (EnableTrace || Context.EnableTrace))
            {
                EventLog.WriteInfo("Trace", "{0}\t[{1},{2}]: {3}", type, Input.Binding.Level, iteration_, Input);
            }
        }
    }
}
