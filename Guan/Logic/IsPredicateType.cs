// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Threading.Tasks;

namespace Guan.Logic
{
    /// <summary>
    /// Predicate type for "is".
    /// </summary>
    internal class IsPredicateType : PredicateType
    {
        class Resolver : GroundPredicateResolver
        {
            public Resolver(CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
            }

            protected override Task<Term> GetNextTermAsync()
            {
                Term term = Input.Arguments[1].Value.ForceEvaluate(Context);

                CompoundTerm result = new CompoundTerm(Singleton, null);
                result.AddArgument(term, "0");
                return Task.FromResult<Term>(result);
            }
        }

        public static readonly IsPredicateType Singleton = new IsPredicateType();

        private IsPredicateType()
            : base("is", true, 2, 2)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(input, constraint, context);
        }
    }
}
