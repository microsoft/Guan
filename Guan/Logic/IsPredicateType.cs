//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System.Threading.Tasks;

    /// <summary>
    /// Predicate type for "is".
    /// </summary>
    internal class IsPredicateType : PredicateType
    {
        public static readonly IsPredicateType Singleton = new IsPredicateType();

        private IsPredicateType()
            : base("is", true, 2, 2)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(input, constraint, context);
        }

        private class Resolver : GroundPredicateResolver
        {
            public Resolver(CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context, 1)
            {
            }

            protected override Task<Term> GetNextTermAsync()
            {
                Term term = this.Input.Arguments[1].Value.ForceEvaluate(this.Context);

                CompoundTerm result = new CompoundTerm(Singleton, null);
                result.AddArgument(term, "0");
                return Task.FromResult<Term>(result);
            }
        }
    }
}
