//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System.Threading.Tasks;

    /// <summary>
    /// Predicate type for "fail"
    /// </summary>
    internal class FailPredicateType : PredicateType
    {
        public static readonly FailPredicateType Singleton = new FailPredicateType();
        public static readonly FailPredicateType NotApplicable = new FailPredicateType();

        private FailPredicateType()
            : base("fail")
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
                return Task.FromResult(false);
            }
        }
    }
}
