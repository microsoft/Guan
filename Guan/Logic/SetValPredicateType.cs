// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// Predicate type for setval.
    /// </summary>
    internal class SetValPredicateType : PredicateType
    {
        class Resolver : BooleanPredicateResolver
        {
            private SetValPredicateType type_;
            private string name_;
            private object oldValue_;

            public Resolver(SetValPredicateType type, CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
                type_ = type;
                name_ = Input.Arguments[0].Value.GetStringValue();
            }

            protected override bool Check()
            {
                if (type_ == Backtrack)
                {
                    oldValue_ = Context[name_];
                }

                Term term = Input.Arguments[1].Value.GetEffectiveTerm();
                Constant constant = term as Constant;
                if (constant == null)
                {
                    throw new GuanException("The 2nd argument of {0} is not a constant: {1}", type_.Name, term);
                }

                Context[name_] = constant.Value;

                return true;
            }

            public override void OnBacktrack()
            {
                if (type_ == Backtrack)
                {
                    Context[name_] = oldValue_;
                }
            }
        }

        public static readonly SetValPredicateType Backtrack = new SetValPredicateType("setval");
        public static readonly SetValPredicateType NoBacktrack = new SetValPredicateType("nb_setval");

        private SetValPredicateType(string name)
            : base(name, true, 2, 2)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(this, input, constraint, context);
        }

        public override void AdjustTerm(CompoundTerm term, Rule rule)
        {
            string name = term.Arguments[0].Value.GetStringValue();
            if (name == null)
            {
                throw new GuanException("The first argument of getval must be string: {0}", term);
            }
        }
    }
}
