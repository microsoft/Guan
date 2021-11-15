// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System.Threading.Tasks;

    /// <summary>
    /// Predicate types for term inspection.
    /// </summary>
    internal class TermPropertyPredicateType : PredicateType
    {
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

        private class Resolver : BooleanPredicateResolver
        {
            private TermPropertyPredicateType type;

            public Resolver(TermPropertyPredicateType type, CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
                this.type = type;
            }

            protected override Task<bool> CheckAsync()
            {
                bool result;
                Term term = this.GetInputArgument(0);
                if (this.type == Var)
                {
                    result = term is Variable;
                }
                else if (this.type == NonVar)
                {
                    result = !(term is Variable);
                }
                else if (this.type == Atom)
                {
                    result = term is Constant;
                }
                else if (this.type == Compound)
                {
                    result = term is CompoundTerm;
                }
                else
                {
                    result = term.IsGround();
                }

                return Task.FromResult(result);
            }
        }
    }
}
