// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Guan.Logic
{
    internal sealed class CallPredicateType : PredicateType
    {
        public static readonly CallPredicateType Singleton = new CallPredicateType();

        private CallPredicateType()
            : base("call", true, 1, 1)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return this.CreateDynamicResolver(input.Arguments[0].Value, constraint, context);
        }
    }
}
