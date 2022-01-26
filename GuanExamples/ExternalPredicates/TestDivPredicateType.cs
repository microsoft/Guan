// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Guan.Logic;
using System;
using System.Threading.Tasks;

namespace GuanExamples
{
    public class TestDivPredicateType : PredicateType
    {
        private static TestDivPredicateType Instance;

        class Resolver : BooleanPredicateResolver
        {
            public Resolver(CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
            }

            protected override Task<bool> CheckAsync()
            {
                double t1 = Convert.ToDouble(Input.Arguments[0].Value.GetEffectiveTerm().GetObjectValue());
                double t2 = Convert.ToDouble(Input.Arguments[1].Value.GetEffectiveTerm().GetObjectValue());
                double result = t1 / t2;
                Console.WriteLine($"divresult: {result}");

                return Task.FromResult(true);
            }
        }

        public static TestDivPredicateType Singleton(string name)
        {
            return Instance ??= new TestDivPredicateType(name);
        }

        // Note the base constructor's arguments minPositionalArguments and maxPositionalArguments. You control the minimum and maximum number of arguments the predicate supports.
        // In this case, rules that employ this external predicate must supply only 2 positional arguments.
        private TestDivPredicateType(string name)
            : base(name, true, 2, 2)
        {

        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(input, constraint, context);
        }
    }
}
