using Guan.Logic;
using GuanExamples.ExternalPredicates;
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

            // predicate name is hard-coded in the predicate's impl ("utcnow"). See GetDateTimeUtcNowPredicateType.cs.
            functorTable.Add(GetDateTimeUtcNowPredicateType.Singleton());

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
                "f1(?a, ?b, ?b, ?a)",
            };

            // List of rules - simple facts and a logic rule to determine if a specified person (?x) is a person.
            var logicRules2 = new List<string>
            {
                "person(adam)",
                "person(betty)",
                "person(carl)",
                "person(dan)",
                "test(?x) :- not(person(?x)), WriteInfo('{0} is not a person', ?x)",
                "test(?x) :- WriteInfo('{0} is a person', ?x)"
             };

            // The purpose of naming a Module is just to be able to identify the module instance if you need to for some reason.
            // Other than that, there is nothing useful about Module name (it is not related to goal naming, for example).
            // Think of name as simply the module id. Nothing more.
            Module module2 = Module.Parse("persontests", logicRules2, null);
            var queryDispatcher2 = new GuanQueryDispatcher(module2);

            // This will return all facts in a comma-delimited string.
            await queryDispatcher2.RunQueryAsync("person(?p)", logicRules2.Count);
            
            // test if specified person is a known fact.
            await queryDispatcher2.RunQueryAsync("test(charles)"); // charles is not a person.
            await queryDispatcher2.RunQueryAsync("test(betty)"); // betty is a person.
            var logicRules3 = new List<string>
            {
                // time() is a system predicate. Used without an arg, it results in DateTime.UtcNow. With a TimeSpan arg, it results in DateTime.UtcNow + arg.
                // Guan will automatically convert a TimeSpan representation (like 1.00:00:00) to a .NET TimeSpan object.
                "testdate(?dt) :- time(-1.00:00:00) > ?dt, WriteInfo('time() impl: {0} is more than 1 day ago', ?dt)",
                // Or, for no good reason other than demonstrating how to write an external predicate that binds a value to rule variable(see GetDateTimeUtcNowPredicateType.cs),
                // use the external predicate utcnow to do the same thing.. utcnow binds a DateTime object to ?n (it is just DateTime.UtcNow).
                // Note that you could use any value for the variable name used in utcnow. You could call it ?now, ?x, whatever..
                "testdate1(?dt) :- utcnow(?n), ?n - ?dt > 1.00:00:00, WriteInfo('utcnow impl: {0} is more than 1 day ago', ?dt)",
                // This will throw a GuanException because the argument for utcnow is grounded, not a variable.
                "testdate2(?dt) :- utcnow(42)",
                // Effectively, else rules.
                "testdate(?dt) :- WriteInfo('time impl: {0} is less than 1 day ago', ?dt)",
                "testdate1(?dt) :- WriteInfo('utcnow impl: {0} is less than 1 day ago', ?dt)",
            };

            Module module3 = Module.Parse("testdates", logicRules3, functorTable);
            var queryDispatcher3 = new GuanQueryDispatcher(module3);

            // Specify query arg as a DateTime object (internally supported by Guan).
            await queryDispatcher3.RunQueryAsync("testdate(DateTime('2022-01-21'))");
            await queryDispatcher3.RunQueryAsync("testdate(DateTime('2022-02-22'))");

            // Employs an external predicate in the rule..
            await queryDispatcher3.RunQueryAsync("testdate1(DateTime('2022-01-21'))");
            await queryDispatcher3.RunQueryAsync("testdate1(DateTime('2022-02-22'))");

            // Crash. Uncomment and run app to see why (or, you could look at the code in GetDateTimeUtcNowPredicateType.GetNextTermAsync function).
            // await queryDispatcher3.RunQueryAsync("testdate2(DateTime('2022-01-25'))");

            Module module = Module.Parse("testx", logicsRules, functorTable);
            var queryDispatcher = new GuanQueryDispatcher(module);

            // test goal with arithmetic external predicates.
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

            // testx goals with internal predicates.
            await queryDispatcher.RunQueryAsync("test4(?x, ?y), test2(?y)"); // (x=1,y=2)
            await queryDispatcher.RunQueryAsync("test5(?x, ?y), test1(?y)"); // (x=2, y=2)
        }
    }
}
