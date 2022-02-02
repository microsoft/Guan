// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Guan.Logic;

namespace GuanExamples
{
    public class GuanQueryDispatcher
    {
        private readonly Module module_;

        public GuanQueryDispatcher(Module module)
        {
            module_ = module;
        }

        public async Task RunQueryAsync(string queryExpression, int maxResults = 1)
        {
            // Required ModuleProvider instance. You created the module used in its construction in Program.cs.
            ModuleProvider moduleProvider = new ModuleProvider();
            moduleProvider.Add(module_);

            // Required QueryContext instance. You must supply moduleProvider (it implements IFunctorProvider).
            QueryContext queryContext = new QueryContext(moduleProvider);

            // The Query instance that will be used to execute the supplied query expression over the related rules.
            Query query = Query.Create(queryExpression, queryContext);

            // Execute the query. 
            // result will be () if there is no answer/result for supplied query (see the simple external predicate rules, for example).
            if (maxResults == 1)
            {
                // Gets one result.
                Term result = await query.GetNextAsync();
                Console.WriteLine($"answer: {result}"); // () if there is no answer.
            }
            else
            {
                // Gets multiple results, if possible, up to supplied maxResults value.
                List<Term> results = await query.GetResultsAsync(maxResults);
                Console.WriteLine($"answer: {string.Join(',', results)}"); // convert the List<Term> object into a comma-delimited string.
            }
        }
    }
}
