// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Guan.Logic
{
    public class GuanQueryDispatcher
    {
        private Module module_;

        public GuanQueryDispatcher(Module module)
        {
            module_ = module;
        }

        public async Task<bool> RunQueryAsync(string queryExpression)
        {
            ResolveOrder order = ResolveOrder.None;
            QueryContext queryContext = new QueryContext();
            queryContext.SetDirection(null, order);
            ModuleProvider moduleProvider = new ModuleProvider();
            moduleProvider.Add(module_);
            
            Query query = Query.Create(
                queryExpression,
                queryContext,
                moduleProvider);

            await query.GetNextAsync().ConfigureAwait(false);
            
            return true;
        }

        public async Task<bool> RunQueryAsync(List<CompoundTerm> queryExpression)
        {
            ResolveOrder order = ResolveOrder.None;
            QueryContext queryContext = new QueryContext();
            queryContext.SetDirection(null, order);
            ModuleProvider moduleProvider = new ModuleProvider();
            moduleProvider.Add(module_);

            Query query = Query.Create(
                queryExpression,
                queryContext,
                moduleProvider);

            await query.GetNextAsync().ConfigureAwait(false);

            return true;
        }
    }
}
