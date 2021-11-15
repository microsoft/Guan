// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System.Threading.Tasks;

    /// <summary>
    /// Predicate type for setval.
    /// </summary>
    internal class SetValPredicateType : PredicateType
    {
        public static readonly SetValPredicateType Backtrack = new SetValPredicateType("b_setval");
        public static readonly SetValPredicateType NoBacktrack = new SetValPredicateType("setval");

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

        private class Resolver : BooleanPredicateResolver
        {
            private SetValPredicateType type;
            private string name;
            private object oldValue;

            public Resolver(SetValPredicateType type, CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
                this.type = type;
                this.name = this.GetInputArgumentString(0);
            }

            public override void OnBacktrack()
            {
                if (this.type == Backtrack)
                {
                    this.Context[this.name] = this.oldValue;
                }
            }

            protected override Task<bool> CheckAsync()
            {
                if (this.type == Backtrack)
                {
                    this.oldValue = this.Context[this.name];
                }

                Term term = this.GetInputArgument(1);
                ReleaseAssert.IsTrue(term.IsGround());
                this.Context[this.name] = term;

                return Task.FromResult(true);
            }
        }
    }
}
