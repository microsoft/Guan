using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// Predicate type for updateobj.
    /// </summary>
    internal class UpdateObjectPredicateType : PredicateType
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
                IWritablePropertyContext context = term1.GetValue() as IWritablePropertyContext;
                if (context == null)
                {
                    throw new GuanException("1st argument of updateobj {0} is not writable context", term1);
                }

                Constant term2 = Input.Arguments[1].Value.GetEffectiveTerm() as Constant;
                string name = term2.GetStringValue();
                if (name == null)
                {
                    throw new GuanException("2nd argument of updateobj {0} is not a string", term2);
                }

                Term term3 = Input.Arguments[2].Value.GetEffectiveTerm();
                context[name] = term3.GetValue();

                return true;
            }
        }

        public static readonly UpdateObjectPredicateType Singleton = new UpdateObjectPredicateType();

        private UpdateObjectPredicateType()
            : base("updateobj", true, 3, 3)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(input, constraint, context);
        }
    }
}
