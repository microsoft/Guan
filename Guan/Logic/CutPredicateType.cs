//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System.Threading.Tasks;

    /// <summary>
    /// Predicate type for cut(!).
    /// </summary>
    internal class CutPredicateType : PredicateType
    {
        public static readonly CutPredicateType Singleton = new CutPredicateType();

        private CutPredicateType()
            : base("!")
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver();
        }

        private class Resolver : BooleanPredicateResolver
        {
            public Resolver()
                : base(null, null, null)
            {
            }

            protected override Task<bool> CheckAsync()
            {
                return Task.FromResult(true);
            }
        }
    }
}
