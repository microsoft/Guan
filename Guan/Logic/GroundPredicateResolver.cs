// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Threading.Tasks;

namespace Guan.Logic
{
    /// <summary>
    /// Resolver that uses grounded term to unify with the goal.
    /// This is typically resolver of external predicate which uses the goal
    /// as a query against concrete instances of data.
    /// </summary>
    public abstract class GroundPredicateResolver : PredicateResolver
    {
        private VariableBinding binding_;

        public GroundPredicateResolver(CompoundTerm input, Constraint constraint, QueryContext context, int max = int.MaxValue)
            : base(input, constraint, context, max)
        {
            binding_ = new VariableBinding(VariableTable.Empty, 0, input.Binding.Level + 1);
        }

        public override async Task<UnificationResult> OnGetNextAsync()
        {
            UnificationResult result = null;
            
            while (result == null && !Completed)
            {
                Term term = await GetNextTermAsync();
                if (term == null)
                {
                    return null;
                }

                if (binding_.Unify(term, Input))
                {
                    result = binding_.CreateOutput();
                }

                binding_.ResetOutput();
            }

            return result;
        }

        protected abstract Task<Term> GetNextTermAsync();
    }
}
