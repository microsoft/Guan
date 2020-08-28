using System.Collections;
using System.Threading.Tasks;

namespace Guan.Logic
{
    /// <summary>
    /// Predicate type for enumeration of collection object.
    /// </summary>
    internal class EnumerablePredicateType : PredicateType
    {
        class Resolver : PredicateResolver
        {
            private IEnumerable collection_;
            private IEnumerator enumerator_;
            private VariableBinding binding_;

            public Resolver(CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
                Term term = input.Arguments[0].Value.GetEffectiveTerm();
                Constant constant = term as Constant;
                if (constant != null)
                {
                    collection_ = constant.Value as IEnumerable;
                }
                else
                {
                    collection_ = term as IEnumerable;
                }

                if (collection_ != null)
                {
                    enumerator_ = collection_.GetEnumerator();
                    binding_ = new VariableBinding(VariableTable.Empty, 0, input.Binding.Level + 1);
                }
            }

            public override Task<UnificationResult> OnGetNextAsync()
            {
                UnificationResult result = null;
                while (result == null && enumerator_ != null && enumerator_.MoveNext())
                {
                    Term term = Term.FromObject(enumerator_.Current);
                    if (binding_.Unify(term, Input.Arguments[1].Value))
                    {
                        result = binding_.CreateOutput();
                    }

                    binding_.ResetOutput();
                }

                return Task.FromResult<UnificationResult>(result);
            }
        }

        public static readonly EnumerablePredicateType Singleton = new EnumerablePredicateType();

        private EnumerablePredicateType()
            : base("enumerable", true, 2, 2)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(input, constraint, context);
        }
    }
}
