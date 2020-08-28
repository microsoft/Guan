using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// Predicate type for assert.
    /// </summary>
    internal class AssertPredicateType : PredicateType
    {
        class Resolver : BooleanPredicateResolver
        {
            private bool append_;

            public Resolver(bool append, CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
                append_ = append;
            }

            protected override bool Check()
            {
                Term term = Input.Arguments[0].Value.GetEffectiveTerm();
                CompoundTerm compound = term as CompoundTerm;
                if (compound == null || !compound.IsGround())
                {
                    throw new GuanException("Argument of assert must be a ground compound term: {0}", term);
                }

                Context.Assert((CompoundTerm)compound.GetGroundedCopy(), append_);

                return true;
            }
        }

        public static readonly AssertPredicateType Assert = new AssertPredicateType("assert");
        public static readonly AssertPredicateType Asserta = new AssertPredicateType("asserta");
        public static readonly AssertPredicateType Assertz = new AssertPredicateType("assertz");

        private AssertPredicateType(string name)
            : base(name, true, 1, 1)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(this != Asserta, input, constraint, context);
        }
    }
}
