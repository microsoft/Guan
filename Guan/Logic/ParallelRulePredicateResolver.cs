//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Resolver that executes rules for the same predicate type in parallel.
    /// </summary>
    internal class ParallelRulePredicateResolver : PredicateResolver
    {
        private Module module;
        private List<Rule> rules;
        private List<RulePredicateResolver> resolvers;
        private List<Task<UnificationResult>> parallelTasks;

        public ParallelRulePredicateResolver(Module module, List<Rule> rules, CompoundTerm input, Constraint constraint, QueryContext context)
            : base(input, constraint, context)
        {
            this.module = module;
            this.rules = rules;
        }

        public override async Task<UnificationResult> OnGetNextAsync()
        {
            this.Context.IsStable = false;

            // Initialize resolvers, contexts and tasks.
            if (this.resolvers == null)
            {
                this.resolvers = new List<RulePredicateResolver>(this.rules.Count);
                this.parallelTasks = new List<Task<UnificationResult>>(this.rules.Count);
                foreach (Rule rule in this.rules)
                {
                    List<Rule> rules = new List<Rule>()
                    {
                        rule
                    };
                    
                    RulePredicateResolver resolver = new RulePredicateResolver(this.module, rules, false, this.Input, this.Constraint, this.Context.CreateChild());
                    this.resolvers.Add(resolver);
                    this.parallelTasks.Add(resolver.GetNextAsync());
                }
            }

            for (int i = 0; i < this.resolvers.Count; i++)
            {
                if (this.parallelTasks[i] != null)
                {
                    this.resolvers[i].Context.IsSuspended = false;
                }
                else
                {
                    this.parallelTasks[i] = this.resolvers[i].GetNextAsync();
                }
            }

            this.Context.IsStable = true;

            UnificationResult result = null;
            while (this.parallelTasks.Count > 0 && result == null)
            {
                Task<UnificationResult> task = await Task.WhenAny(this.parallelTasks);
                result = await task;
                int index = this.parallelTasks.IndexOf(task);

                if (result != null)
                {
                    this.parallelTasks[index] = null;
                    // Suspend all unfinished ones
                    for (int i = 0; i < this.resolvers.Count; i++)
                    {
                        if (i != index)
                        {
                            this.resolvers[i].Context.IsSuspended = true;
                        }
                    }
                }
                else
                {
                    this.parallelTasks.RemoveAt(index);
                    this.resolvers.RemoveAt(index);
                }
            }

            return result;
        }

        protected override void OnCancel()
        {
            foreach (RulePredicateResolver resolver in this.resolvers)
            {
                resolver.Context.IsCancelled = true;
            }
        }
    }
}
