// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    /// <summary>
    /// Functor that can transform the containing compound term.
    /// </summary>
    internal class EvaluatedFunctor : Functor
    {
        private GuanFunc func;
        private ConstraintPredicateType constraintType;

        public EvaluatedFunctor(GuanFunc func)
            : base(func.Name)
        {
            this.func = func;
            this.constraintType = new ConstraintPredicateType(this);
        }

        public GuanFunc Func
        {
            get
            {
                return this.func;
            }
        }

        public ConstraintPredicateType ConstraintType
        {
            get
            {
                return this.constraintType;
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

                if (arg is CompoundTerm)
                {
                    throw new GuanException($"Evaluated function {term} has unexpected argument {arg}");
                }

                args[i] = arg.GetObjectValue();
            }

            object result = this.func.Invoke(context, args);
            return Term.FromObject(result);
        }
    }
}
