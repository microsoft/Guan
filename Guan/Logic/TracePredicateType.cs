// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Guan.Logic
{
    /// <summary>
    /// Predicate type for trace.
    /// </summary>
    internal class TracePredicateType : PredicateType
    {
        class Resolver : BooleanPredicateResolver
        {
            private bool enable_;
            private bool oldValue_;

            public Resolver(bool enable, QueryContext context)
                : base(null, null, context)
            {
                enable_ = enable;
            }

            protected override bool Check()
            {
                oldValue_ = Context.EnableTrace;
                Context.EnableTrace = enable_;
                return true;
            }

            public override void OnBacktrack()
            {
                Context.EnableTrace = oldValue_;
            }
        }

        public static readonly TracePredicateType Enable = new TracePredicateType("trace");
        public static readonly TracePredicateType Disable = new TracePredicateType("notrace");

        private TracePredicateType(string name)
            : base(name)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(this == Enable, context);
        }
    }
}
