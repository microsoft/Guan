using System.Collections.Generic;
using System.Threading.Tasks;

namespace Guan.Logic
{
    /// <summary>
    /// Resolver that executes rules for the same predicate type in parallel.
    /// </summary>
    internal class ParallelRulePredicateResolver : PredicateResolver
    {
        private Module module_;
        private List<Rule> rules_;
        private List<RulePredicateResolver> resolvers_;
        private List<Task<UnificationResult>> parallelTasks_;

        public ParallelRulePredicateResolver(Module module, List<Rule> rules, CompoundTerm input, Constraint constraint, QueryContext context)
            : base(input, constraint, context)
        {
            module_ = module;
            rules_ = rules;
        }

        public override async Task<UnificationResult> OnGetNextAsync()
        {
            Context.IsStable = false;

            // Initialize resolvers, contexts and tasks.
            if (resolvers_ == null)
            {
                resolvers_ = new List<RulePredicateResolver>(rules_.Count);
                parallelTasks_ = new List<Task<UnificationResult>>(rules_.Count);
                foreach (Rule rule in rules_)
                {
                    List<Rule> rules = new List<Rule>
                    {
                        rule
                    };
                    RulePredicateResolver resolver = new RulePredicateResolver(module_, rules, false, Input, Constraint, Context.CreateChild());
                    resolvers_.Add(resolver);
                    parallelTasks_.Add(resolver.GetNextAsync());
                }
            }

            for (int i = 0; i < resolvers_.Count; i++)
            {
                if (parallelTasks_[i] != null)
                {
                    resolvers_[i].Context.IsSuspended = false;
                }
                else
                {
                    parallelTasks_[i] = resolvers_[i].GetNextAsync();
                }
            }

            Context.IsStable = true;

            UnificationResult result = null;
            while (parallelTasks_.Count > 0 && result == null)
            {
                Task<UnificationResult> task = await Task.WhenAny(parallelTasks_);
                result = task.Result;
                int index = parallelTasks_.IndexOf(task);

                if (result != null)
                {
                    parallelTasks_[index] = null;
                    // Suspend all unfinished ones
                    for (int i = 0; i < resolvers_.Count; i++)
                    {
                        if (i != index)
                        {
                            resolvers_[i].Context.IsSuspended = true;
                        }
                    }
                }
                else
                {
                    parallelTasks_.RemoveAt(index);
                    resolvers_.RemoveAt(index);
                }
            }

            return result;
        }

        protected override void OnCancel()
        {
            foreach (RulePredicateResolver resolver in resolvers_)
            {
                resolver.Context.IsCancelled = true;
            }
        }
    }
}
