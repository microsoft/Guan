## Getting Started with Guan

Guan, which is built as a ```.NET Standard Library```, is designed to be used as part of an existing application (.NET Core, .NET Desktop) where there is a need for logic programming. The consuming program
can either house logic rules in files or supply them as string arguments directly to Guan. The purpose of this document is show how to use Guan within an application.  

### Installing Guan 

You can install Guan into an existing application by adding a ```PackageReference``` to the consuming Project's csproj file: 

``` XML
<ItemGroup>
    <PackageReference Include="Microsoft.Logic.Guan" Version="1.0.3" />
    ...
</ItemGroup>
```
### Using Guan from external code
Within a code file (.cs) you must reference ```Guan.Logic``` in a using statement:

```C#
using Guan.Logic;
```
Guan supports the usage of Prolog-like logic rules (textual query expression) that employ the standard format: 

A rule ```head``` which identifies a ```goal``` and series of ```sub-rules (or sub-goals)``` that form the logical workflow.

```Prolog
goal() :- subgoal1, subgoal2
```

In Guan, ```goal``` is implemented as a ```CompoundTerm``` object. It can have any number of arguments (variables), which form the ```CompoundTerm.Arguments``` property, which is a ```List<TermArgument>```. We will revisit this later. 

Let's create a simple program (.NET Core 3.1 Console app) with very simple rules and a few external predicates. You can run this program by building and running the [GuanExamples](/GuanExamples) project.

```C#
using Guan.Logic;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GuanExamples
{
    class Program
    {
        static async Task Main()
        {
            // External predicate types (as object instances or singletons (static single instances)) must be specified in a Guan FunctorTable object
            // which is used to create the Module, which is a collection of predicate types and is required by the Query executor (see RunQueryAsync impl in GuanQueryDispatcher.cs).
            var functorTable = new FunctorTable();
            functorTable.Add(TestAddPredicateType.Singleton("addresult"));
            functorTable.Add(TestDivPredicateType.Singleton("divresult"));
            functorTable.Add(TestSubPredicateType.Singleton("subresult"));

            // Create a List<string> containing logic rules (these could also be housed in a text file). These rules are very simple by design, as are the external predicates used in this sample console app.
            // The rules below are simple examples of using logic in their sub rule parts and calling an external predicate.
            var logicsRules = new List<string>
            {
                "test(?x, ?y) :- ?x == ?y, addresult(?x, ?y)",
                "test(?x, ?y) :- ?y > 0 && ?y < ?x, divresult(?x, ?y)",
                "test(?x, ?y) :- ?x > ?y, subresult(?x, ?y)",
                "test1(1)",
                "test1(2)",
                "test2(2)",
                "test2(3)",
                "test3(q(1, ?x))",
                "test4(?x, ?y) :- test3(q(?x, ?y)), test1(?y)",
                "test5(?x, ?y) :- ?x > 1 && ?y < 3, test1(?x), test2(?y)",
                "test6(?x) :- test1(?x), not(test2(?x))",
                "test7(?x) :- not(?x < 2), test1(?x)",
                "test8(?x, ?y, ?z) :- showtype(?x, ?y), ?x = 5, showtype(?x, ?z)",
                "test9(?x, ?y, ?z) :- b_setval(v1, 0), test1(?x), getval(v1, ?y), b_setval(v1, 5), getval(v1, ?z)",
                "test10(?x, ?y, ?z) :- b_setval(v1, 0), test1(?x), getval(v1, ?y), setval(v1, 5), getval(v1, ?z)",
                "showtype(?x, 'var') :- var(?x)",
                "showtype(?x, 'nonvar') :- nonvar(?x)",
                "showtype(?x, 'atom') :- atom(?x)",
                "showtype(?x, 'compound') :- compound(?x)",
                "f1(?a, ?b, ?b, ?a)"
            };

            // A Module is a collection of predicate types.
            Module module = Module.Parse("test", logicsRules, functorTable);
            var queryDispatcher = new GuanQueryDispatcher(module);

            /* Execute queries via GuanQueryDispatcher helper class */

            // test goal with external predicate impls.
            await queryDispatcher.RunQueryAsync("test(3, 3)");
            await queryDispatcher.RunQueryAsync("test(0, 0)");
            await queryDispatcher.RunQueryAsync("test(5, 2)");
            await queryDispatcher.RunQueryAsync("test(3, 2)");
            await queryDispatcher.RunQueryAsync("test(4, 2)");
            await queryDispatcher.RunQueryAsync("test(2, 5)");
            await queryDispatcher.RunQueryAsync("test(6, 2)");
            await queryDispatcher.RunQueryAsync("test(8, 2)");
            await queryDispatcher.RunQueryAsync("test(25, 5)");
            await queryDispatcher.RunQueryAsync("test(1, 0)");

            // testx goals with internal predicate impls.
            // the answer/result for the below query would be (x=1,y=2) given the rules.
            await queryDispatcher.RunQueryAsync("test4(?x, ?y), test2(?y)", true);
        }
    }
}
``` 

There are three main pieces to executing a Guan query:

- A repair rule or query expression that Guan will parse and execute.
- A Module that contains a list of rules and optionally a ```FunctorTable``` instance that holds a list of predicate types (```FunctorTable``` only applies if your query expressions (rules) employ External Predicates).
- An instance of Guan's Query type created via the static Query.Create function, which takes rules, a ```QueryContext``` instance, and a ```ModuleProvider``` instance, which is created with the ```Module(s)``` you define. 

Let's look at the simple helper class in the ```GuanExamples``` project, ```GuanQueryDispatcher```, to make this more clear.  

```C#
using System.Collections.Generic;
using System.Threading.Tasks;
using Guan.Logic;

namespace GuanExamples
{
    public class GuanQueryDispatcher
    {
        // This is set by the consumer of this type via this class's constructor.        
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
            // result will be () if there is no answer/result for supplied query 
            // (see the simple external predicate rules, for example).
            Term result = await query.GetNextAsync();

            if (showResult)
            {
                Console.WriteLine($"answer: {result}");
            }
        }
    }
}
```

This is pretty straightforward and it is expected that you already have experience with ```Prolog```, so let's move on to the External Predicates used in this simple application.  

There are 3 external predicates used here:

- ```TestAddPredicateType```
- ```TestDivPredicateType```
- ```TestSubPredicateType```

Let's look the implementation for ```TestAddPredicateType``` (the other two are exactly the same in structure):  

```C#
using Guan.Logic;
using System;
using System.Threading.Tasks;

namespace GuanExamples
{
    public class TestAddPredicateType : PredicateType
    {
        private static TestAddPredicateType Instance;

        class Resolver : BooleanPredicateResolver
        {
            public Resolver(CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
            }

            protected override Task<bool> CheckAsync()
            {
                // This is the value of the first argument passed to the external predicate addresult().
                long t1 = (long)Input.Arguments[0].Value.GetEffectiveTerm().GetObjectValue();
    
                // This is the value of the second argument passed to the external predicate addresult().
                long t2 = (long)Input.Arguments[1].Value.GetEffectiveTerm().GetObjectValue();
    
                // Do something with argunent values (in this case simply add them together).
                long result = t1 + t2;

                // Call an external (to Guan) API that does something with the result.
                Console.WriteLine($"addresult: {result}");

                // BooleanPredicateResolver type always supplies or binds a boolean result.
                return Task.FromResult(true);
            }
        }

        public static TestAddPredicateType Singleton(string name)
        {
            // ??= is C#'s null-coalescing assignment operator. It is convenience syntax that assigns the value of
            // its right-hand operand to its left-hand operand only if the left-hand operand evaluates to null. 
            // The ??= operator does not evaluate its right-hand operand if the left-hand operand evaluates to non-null.
            return Instance ??= new TestAddPredicateType(name);
        }

        // Note the base constructor's arguments minPositionalArguments and maxPositionalArguments.
        // You control the minimum and maximum number of arguments the predicate supports.
        // In this case, rules that employ this external predicate must supply only 2 positional arguments.
        private TestAddPredicateType(string name)
            : base(name, true, 2, 2)
        {

        }

        // override to create the Resolver instance.
        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(input, constraint, context);
        }
    }
}
```

Note that any external predicate must derive from the ```PredicateType``` base class. Further, an external predicate must contain an internal Resolver class that derives from any supported ```Resolver``` type. 
In this case, all 3 external predicates derive from BooleanPredicateResolver, which means the result of their execution will be bound to a boolean (and in this case, that value is always true).
The key function for you here is ```CheckAsync()```. This is basically the entry point to your custom external implementation (external to the rule from which it is called). 

```C#
protected override Task<bool> CheckAsync()
{
    // This is the value of the first argument passed to the external predicate addresult().
    long t1 = (long)Input.Arguments[0].Value.GetEffectiveTerm().GetObjectValue();
    
    // This is the value of the second argument passed to the external predicate addresult().
    long t2 = (long)Input.Arguments[1].Value.GetEffectiveTerm().GetObjectValue();
    
    // Do something with argunent values (in this case simply add them together).
    long result = t1 + t2;

    // Call an external (to Guan) API that does something with the result.
    Console.WriteLine($"addresult: {result}");

    // BooleanPredicateResolver type always supplies or binds a boolean result.
    return Task.FromResult(true);
}
```

