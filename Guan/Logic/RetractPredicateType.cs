// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal class RetractPredicateType : PredicateType
    {
        public static readonly RetractPredicateType Singleton = new RetractPredicateType();

        private RetractPredicateType()
            : base("retract", true, 1, 1)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(input, constraint, context);
        }

        private class Resolver : PredicateResolver
        {
            private CompoundTerm goal;
            private PredicateType predicateType;
            private List<Rule> rules;
            private int current;

            public Resolver(CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
                this.goal = this.GetInputArgument(0).ToCompound();
                if (this.goal == null)
                {
                    throw new GuanException($"Invalid argument for retract {this.GetInputArgument(0)}");
                }

                this.current = 0;
                this.predicateType = context.DynamicModule.GetPredicateType(this.goal.Functor.Name);
                if (this.predicateType != null)
                {
                    this.rules = new List<Rule>(this.predicateType.Rules);
                }
            }

            public override Task<UnificationResult> OnGetNextAsync()
            {
                UnificationResult result = null;
                while (this.rules != null && this.current < this.rules.Count && (result == null))
                {
                    VariableBinding binding = this.rules[this.current].CreateBinding(this.Input.Binding.Level + 1);
                    if (binding.Unify(this.rules[this.current].Head.DuplicateGoal(binding), this.goal))
                    {
                        result = binding.CreateOutput();
                        this.predicateType.Rules.Remove(this.rules[this.current]);
                    }

                    this.current++;
                }

                return Task.FromResult(result);
            }
        }
    }
}
