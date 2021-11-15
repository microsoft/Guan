// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System.Collections;
    using System.Threading.Tasks;

    /// <summary>
    /// Predicate type for enumeration of collection object.
    /// </summary>
    internal class EnumerablePredicateType : PredicateType
    {
        public static readonly EnumerablePredicateType Singleton = new EnumerablePredicateType();

        private EnumerablePredicateType()
            : base("enumerable", true, 2, 2)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(input, constraint, context);
        }

        private class Resolver : PredicateResolver
        {
            private IEnumerable collection;
            private IEnumerator enumerator;
            private VariableBinding binding;

            public Resolver(CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
                Term term = input.Arguments[0].Value.GetEffectiveTerm();
                Constant constant = term as Constant;
                if (constant != null)
                {
                    this.collection = constant.Value as IEnumerable;
                }
                else
                {
                    this.collection = term as IEnumerable;
                }

                if (this.collection != null)
                {
                    this.enumerator = this.collection.GetEnumerator();
                    this.binding = new VariableBinding(VariableTable.Empty, 0, input.Binding.Level + 1);
                }
            }

            public override Task<UnificationResult> OnGetNextAsync()
            {
                UnificationResult result = null;
                while (result == null && this.enumerator != null && this.enumerator.MoveNext())
                {
                    Term term = Term.FromObject(this.enumerator.Current);
                    if (this.binding.Unify(term, this.Input.Arguments[1].Value))
                    {
                        result = this.binding.CreateOutput();
                    }

                    this.binding.ResetOutput();
                }

                return Task.FromResult<UnificationResult>(result);
            }
        }
    }
}
