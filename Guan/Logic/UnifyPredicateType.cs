//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System.Threading.Tasks;

    /// <summary>
    /// Predicate type for explicit unification.
    /// </summary>
    internal class UnifyPredicateType : PredicateType
    {
        public static readonly UnifyPredicateType Regular = new UnifyPredicateType("=");

        private UnifyPredicateType(string name)
            : base(name, true, 2, 2)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(input, constraint, context);
        }

        private class Resolver : BooleanPredicateResolver
        {
            public Resolver(CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
            }

            protected override Task<bool> CheckAsync()
            {
                Term term1 = this.GetInputArgument(0);
                Term term2 = this.GetInputArgument(1);

                return Task.FromResult(term1.Unify(term2));
            }
        }
    }
}
