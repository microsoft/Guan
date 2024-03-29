﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System.Threading.Tasks;

    /// <summary>
    /// Predicate type for forwardcut.
    /// </summary>
    internal class ForwardCutPredicateType : PredicateType
    {
        public static readonly ForwardCutPredicateType Singleton = new ForwardCutPredicateType();

        private ForwardCutPredicateType()
            : base("forwardcut", true, 0, 0)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(context);
        }

        public override void AdjustTerm(CompoundTerm term, Rule rule)
        {
            base.AdjustTerm(term, rule);

            int i;
            for (i = 0; i < rule.Goals.Count && term != rule.Goals[i]; i++)
            {
            }

            if (i == 0 || i >= rule.Goals.Count - 1 || rule.Goals[i - 1].PredicateType is ConstraintPredicateType)
            {
                throw new GuanException("Invalid use of forwardcut");
            }
        }

        private class Resolver : BooleanPredicateResolver
        {
            public Resolver(QueryContext context)
                : base(null, null, context)
            {
            }

            protected override Task<bool> CheckAsync()
            {
                return Task.FromResult(true);
            }
        }
    }
}
