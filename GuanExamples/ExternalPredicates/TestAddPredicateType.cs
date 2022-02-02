// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

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
