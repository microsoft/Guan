// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal class QueryToListPredicateType : PredicateType
    {
        public static readonly QueryToListPredicateType Singleton = new QueryToListPredicateType();

        private QueryToListPredicateType()
            : base("query_to_list", true, 2, 2)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(input, constraint, context);
        }

        private class Resolver : GroundPredicateResolver
        {
            public Resolver(CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context, 1)
            {
                Term goal = this.GetInputArgument(0);
                ReleaseAssert.IsTrue(goal.IsGround() && goal is CompoundTerm, "query_to_list first argument {0} is not ground", goal);
                ReleaseAssert.IsTrue(this.GetInputArgument(1) is Variable, "query_to_list second argument {0} is not variable", this.GetInputArgument(1));
            }

            protected override async Task<Term> GetNextTermAsync()
            {
                CompoundTerm goal = (CompoundTerm)this.GetInputArgument(0);

                QueryContext context = new QueryContext(null);
                VariableTable table = new VariableTable();
                table.GetIndex("_result", true);
                VariableBinding binding = new VariableBinding(table, 0, this.Input.Binding.Level + 1);
                goal = goal.DuplicateGoal(binding);
                goal.AddArgument(binding.GetLocalVariable(0), "this");

                List<CompoundTerm> results = new List<CompoundTerm>();
                PredicateResolver resolver = goal.PredicateType.CreateResolver(goal, null, context);
                UnificationResult result = await resolver.GetNextAsync();
                while (this.AddResult(results, result))
                {
                    result = await resolver.GetNextAsync();
                }

                CompoundTerm compund = new CompoundTerm(this.Input.Functor);
                compund.AddArgument(ListTerm.FromEnumerable(results), "1");

                return compund;
            }

            private bool AddResult(List<CompoundTerm> results, UnificationResult result)
            {
                if (result == null)
                {
                    return false;
                }

                if (result.Entries.Count > 0)
                {
                    results.Add((CompoundTerm)result.Entries[0].GetEffectiveTerm());
                }

                return true;
            }
        }
    }
}
