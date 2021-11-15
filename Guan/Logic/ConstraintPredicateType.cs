// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Threading.Tasks;

    internal class ConstraintPredicateType : PredicateType
    {
        private EvaluatedFunctor func;

        public ConstraintPredicateType(EvaluatedFunctor evaluatedFunctor)
            : base(evaluatedFunctor.Name)
        {
            this.func = evaluatedFunctor;
        }

        public EvaluatedFunctor Func
        {
            get
            {
                return this.func;
            }
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(this.func, input, constraint, context);
        }

        private class Resolver : PredicateResolver
        {
            private EvaluatedFunctor func;

            public Resolver(EvaluatedFunctor func, CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
                this.func = func;
            }

            public override Task<UnificationResult> OnGetNextAsync()
            {
                UnificationResult result = null;
                if (this.Iteration == 0)
                {
                    Term term = this.func.Evaluate(this.Input, this.Context);
                    Constant constant = term as Constant;
                    if (constant == null)
                    {
                        result = new UnificationResult(0);
                        result.AddConstraint(this.Input);
                    }
                    else if (constant.IsTrue())
                    {
                        result = UnificationResult.Empty;
                    }

                    this.Complete();
                }

                return Task.FromResult(result);
            }
        }
    }
}
