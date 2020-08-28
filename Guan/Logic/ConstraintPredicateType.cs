using System.Threading.Tasks;

namespace Guan.Logic
{
    internal class ConstraintPredicateType : PredicateType
    {
        class Resolver : PredicateResolver
        {
            private EvaluatedFunctor func_;

            public Resolver(EvaluatedFunctor func, CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
                func_ = func;
            }

            public override Task<UnificationResult> OnGetNextAsync()
            {
                UnificationResult result = null;
                if (Iteration == 0)
                {
                    Term term = func_.Evaluate(Input, Context);
                    Constant constant = term as Constant;
                    if (constant == null)
                    {
                        result = new UnificationResult(0);
                        result.AddConstraint(Input);
                    }
                    else if (constant.IsTrue())
                    {
                        result = UnificationResult.Empty;
                    }

                    Complete();
                }

                return Task.FromResult(result);
            }
        }

        private EvaluatedFunctor func_;

        public ConstraintPredicateType(EvaluatedFunctor evaluatedFunctor)
            : base(evaluatedFunctor.Name)
        {
            func_ = evaluatedFunctor;
        }

        public EvaluatedFunctor Func
        {
            get
            {
                return func_;
            }
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(func_, input, constraint, context);
        }
    }
}
