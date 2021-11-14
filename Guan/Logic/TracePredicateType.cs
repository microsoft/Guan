//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System.Threading.Tasks;

    /// <summary>
    /// Predicate type for trace.
    /// </summary>
    internal class TracePredicateType : PredicateType
    {
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

        private class Resolver : BooleanPredicateResolver
        {
            private bool enable;
            private bool oldValue;

            public Resolver(bool enable, QueryContext context)
                : base(null, null, context)
            {
                this.enable = enable;
            }

            public override void OnBacktrack()
            {
                this.Context.EnableTrace = this.oldValue;
            }

            protected override Task<bool> CheckAsync()
            {
                this.oldValue = this.Context.EnableTrace;
                this.Context.EnableTrace = this.enable;
                return Task.FromResult(true);
            }
        }
    }
}
