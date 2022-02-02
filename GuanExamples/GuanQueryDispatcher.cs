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

        public async Task RunQueryAsync(string queryExpression, bool showResult = false)
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
            Term result = await query.GetNextAsync();

            if (showResult)
            {
                Console.WriteLine($"answer: {result}");
            }
        }
    }
}
