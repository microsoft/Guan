// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System.Threading.Tasks;

    /// <summary>
    /// Resolver that acts as a boolean condition check with empty output.
    /// </summary>
    public abstract class BooleanPredicateResolver : PredicateResolver
    {
        public BooleanPredicateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
            : base(input, constraint, context, 1)
        {
        }

        public override async Task<UnificationResult> OnGetNextAsync()
        {
            UnificationResult result;
            if (await this.CheckAsync())
            {
                result = UnificationResult.Empty;
            }
            else
            {
                result = null;
            }

            return result;
        }

        protected abstract Task<bool> CheckAsync();
    }
}
