// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Guan.Logic;

namespace GuanExamples.ExternalPredicates
{
    public class GetDateTimeUtcNowPredicateType : PredicateType
    {
        private static GetDateTimeUtcNowPredicateType Instance;

        /// <summary>
        /// GroundPredicateResolver is typically a resolver of an external predicate which uses the goal
        /// as a query against concrete instances of data.
        /// </summary>
        private class Resolver : GroundPredicateResolver
        {
            public Resolver(CompoundTerm input, Constraint constraint, QueryContext context)
                    : base(input, constraint, context, 1)
            {

            }

            protected override Task<Term> GetNextTermAsync()
            {
                // The goal here is to bind a value to the single supplied argument for use in a rule that employs this external predicate.
                // This argument must be a variable and a variable is by definition not grounded. In fact, this argument is an IndexedVariable.
                // This is an example of how to validate input in a predicate that requires specific input types.
                if (base.GetInputArgument(0).IsGround())
                {
                    throw new GuanException("The first argument of utcnow must be a variable: {0} was supplied.", this.GetInputArgument(0));
                }

                var currentTime = DateTime.UtcNow;
                var result = new CompoundTerm(Input.Functor);

                // "0" is used here so that the variable name in the rule that employs this predicate can be named whatever you want.
                // This predicate only supports one argument, which is the variable (an IndexedVariable) that will hold the result.
                result.AddArgument(new Constant(currentTime), "0");

                return Task.FromResult(result.GetEffectiveTerm());
            }
        }

        public static GetDateTimeUtcNowPredicateType Singleton()
        {
            return Instance ??= new GetDateTimeUtcNowPredicateType("utcnow");
        }
        
        // This predicate only supports one argument, which is the variable (an IndexedVariable) that will hold the result.
        private GetDateTimeUtcNowPredicateType(string name)
                 : base(name, true, 1, 1)
        {

        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(input, constraint, context);
        }
    }
}
