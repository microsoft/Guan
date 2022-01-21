using System;
using System.Collections.Generic;
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

        public async Task RunQueryAsync(string queryExpression, bool showResult = false)
        {
            // Required QueryContext instance.
            QueryContext queryContext = new QueryContext();

            // Required ModuleProvider instance. You created the module used in its construction in Program.cs.
            ModuleProvider moduleProvider = new ModuleProvider();
            moduleProvider.Add(module_);

            // The Query instance that will be used to execute the supplied logic rule, queryExpression arg.
            Query query = Query.Create(queryExpression, queryContext, moduleProvider);

            // Execute the query. 
            // result will be () if there is no answer/result for supplied query (see the simple external predicate rules, for example).
            Term result = await query.GetNextAsync();

            if (showResult)
            {
                Console.WriteLine($"answer: {result}");
            }
        }
    }
}
