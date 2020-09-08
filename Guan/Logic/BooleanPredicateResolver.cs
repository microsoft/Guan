// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Threading.Tasks;

namespace Guan.Logic
{
    /// <summary>
    /// Resolver that acts as a boolean condition check with empty output.
    /// </summary>
    public abstract class BooleanPredicateResolver : PredicateResolver
    {
        public BooleanPredicateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
            : base(input, constraint, context, 1)
        {
        }

        protected abstract bool Check();

        public override Task<UnificationResult> OnGetNextAsync()
        {
            UnificationResult result;
            if (Check())
            {
                result = UnificationResult.Empty;
            }
            else
            {
                result = null;
            }

            return Task.FromResult<UnificationResult>(result);
        }
    }
}
