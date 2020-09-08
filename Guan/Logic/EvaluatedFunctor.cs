// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// Functor that can transform the containing compound term.
    /// </summary>
    internal class EvaluatedFunctor : Functor
    {
        private GuanFunc func_;
        private ConstraintPredicateType constraintType_;

        public EvaluatedFunctor(GuanFunc func)
            : base(func.Name)
        {
            func_ = func;
            constraintType_ = new ConstraintPredicateType(this);
        }

        public GuanFunc Func
        {
            get
            {
                return func_;
            }
        }

        public ConstraintPredicateType ConstraintType
        {
            get
            {
                return constraintType_;
            }
        }

        public Term Evaluate(CompoundTerm term, QueryContext context)
        {
            object[] args = new object[term.Arguments.Count];
            for (int i = 0; i < args.Length; i++)
            {
                Term arg = term.Arguments[i].Value.GetEffectiveTerm();
                if (!arg.IsGround())
                {
                    return term;
                }

                args[i] = arg.GetValue();
            }

            object result = func_.Invoke(context, args);
            return Term.FromObject(result);
        }
    }
}
