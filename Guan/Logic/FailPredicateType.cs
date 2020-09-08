namespace Guan.Logic
{
    /// <summary>
    /// Predicate type for "fail"
    /// </summary>
    internal class FailPredicateType : PredicateType
    {
        class Resolver : BooleanPredicateResolver
        {
            public Resolver()
                : base(null, null, null)
            {
            }

            protected override bool Check()
            {
                return false;
            }
        }

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
    }
}
