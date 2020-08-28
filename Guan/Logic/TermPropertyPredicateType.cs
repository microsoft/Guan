// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Guan.Logic
{
    /// <summary>
    /// Predicate types for term inspection.
    /// </summary>
    internal class TermPropertyPredicateType : PredicateType
    {
        class Resolver : BooleanPredicateResolver
        {
            private TermPropertyPredicateType type_;

            public Resolver(TermPropertyPredicateType type, CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
                type_ = type;
            }

            protected override bool Check()
            {
                Term term = Input.Arguments[0].Value.GetEffectiveTerm();
                if (type_ == Var)
                {
                    return term is Variable;
                }
                else if (type_ == NonVar)
                {
                    return !(term is Variable);
                }
                else if (type_ == Atom)
                {
                    return term is Constant;
                }
                else if (type_ == Compound)
                {
                    return term is CompoundTerm;
                }
                else
                {
                    return term.IsGround();
                }
            }
        }

        public static readonly TermPropertyPredicateType Var = new TermPropertyPredicateType("var");
        public static readonly TermPropertyPredicateType NonVar = new TermPropertyPredicateType("nonvar");
        public static readonly TermPropertyPredicateType Atom = new TermPropertyPredicateType("atom");
        public static readonly TermPropertyPredicateType Compound = new TermPropertyPredicateType("compound");
        public static readonly TermPropertyPredicateType Ground = new TermPropertyPredicateType("ground");

        private TermPropertyPredicateType(string name)
            : base(name, true, 1, 1)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(this, input, constraint, context);
        }
    }
}
