// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System.Threading.Tasks;

    /// <summary>
    /// Resolver that uses grounded term to unify with the goal.
    /// This is typically resolver of external predicate which uses the goal
    /// as a query against concrete instances of data.
    /// </summary>
    public abstract class GroundPredicateResolver : PredicateResolver
    {
        private VariableBinding binding;
        private int count;

        protected GroundPredicateResolver(CompoundTerm input, Constraint constraint, QueryContext context, int max = int.MaxValue)
            : base(input, constraint, context, max)
        {
            this.binding = new VariableBinding(VariableTable.Empty, 0, input.Binding.Level + 1);
            this.count = 0;
        }

        public override async Task<UnificationResult> OnGetNextAsync()
        {
            UnificationResult result = null;

            while (result == null && this.count < this.Max && !this.Completed)
            {
                Term term = await this.GetNextTermAsync();
                if (term == null)
                {
                    return null;
                }

                if (this.binding.Unify(term, this.Input))
                {
                    result = this.binding.CreateOutput();
                }

                this.count++;

                this.binding.ResetOutput();
            }

            return result;
        }

        protected abstract Task<Term> GetNextTermAsync();
    }
}
