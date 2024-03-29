﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Predicate type for assert.
    /// </summary>
    internal class AssertPredicateType : PredicateType
    {
        public static readonly AssertPredicateType Assert = new AssertPredicateType("assert");
        public static readonly AssertPredicateType Asserta = new AssertPredicateType("asserta");
        public static readonly AssertPredicateType Assertz = new AssertPredicateType("assertz");

        private AssertPredicateType(string name)
            : base(name, true, 1, 1)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(this != Asserta, input, constraint, context);
        }

        private class Resolver : BooleanPredicateResolver
        {
            private bool append;

            public Resolver(bool append, CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
                this.append = append;
            }

            protected override Task<bool> CheckAsync()
            {
                this.Context.DynamicModule.Add(this.GetInputArgument(0).GetEffectiveTerm(), this.append);

                return Task.FromResult(true);
            }
        }
    }
}
