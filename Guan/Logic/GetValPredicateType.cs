using System.Threading.Tasks;
using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// Predicate type for getval.
    /// </summary>
    internal class GetValPredicateType : PredicateType
    {
        class Resolver : GroundPredicateResolver
        {
            public Resolver(CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context, 1)
            {
            }

            protected override Task<Term> GetNextTermAsync()
            {
                string name = Input.Arguments[0].Value.GetStringValue();
                object value = Context[name];

                CompoundTerm result = new CompoundTerm(GetValPredicateType.Singleton, null);
                result.AddArgument(new Constant(value), "1");
                return Task.FromResult<Term>(result);
            }
        }

        public static readonly GetValPredicateType Singleton = new GetValPredicateType();

        private GetValPredicateType()
            : base("getval", true, 2, 2)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(input, constraint, context);
        }

        public override void AdjustTerm(CompoundTerm term, Rule rule)
        {
            string name = term.Arguments[0].Value.GetStringValue();
            if (name == null)
            {
                throw new GuanException("The first argument of getval must be string: {0}", term);
            }

            if (!(term.Arguments[1].Value is IndexedVariable))
            {
                throw new GuanException("The second argument of getval must be a variable: {0}", term);
            }
        }
    }
}
