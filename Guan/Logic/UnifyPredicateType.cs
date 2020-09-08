// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Guan.Logic
{
    /// <summary>
    /// Predicate type for explicit unification.
    /// </summary>
    internal class UnifyPredicateType : PredicateType
    {
        class Resolver : BooleanPredicateResolver
        {
            public Resolver(CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
            }

            protected override bool Check()
            {
                Term term1 = Input.Arguments[0].Value.GetEffectiveTerm();
                Term term2 = Input.Arguments[1].Value.GetEffectiveTerm();

                return term1.Unify(term2);
            }
        }

        public static readonly UnifyPredicateType Regular = new UnifyPredicateType("=");

        private UnifyPredicateType(string name)
            : base(name, true, 2, 2)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(input, constraint, context);
        }
    }
}
