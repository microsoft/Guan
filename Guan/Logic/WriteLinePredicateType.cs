// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// Predicate type for writing a line to log.
    /// </summary>
    internal class WriteLinePredicateType : PredicateType
    {
        class Resolver : BooleanPredicateResolver
        {
            public Resolver(CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
            }

            protected override bool Check()
            {
                string format = Input.Arguments[0].Value.GetEffectiveTerm().GetStringValue();
                object[] args = new object[Input.Arguments.Count - 1];
                for (int i = 1; i < Input.Arguments.Count; i++)
                {
                    args[i - 1] = Input.Arguments[i].Value.GetEffectiveTerm();
                }

                EventLog.WriteInfo("WriteLine", format, args);

                return true;
            }
        }

        public static readonly WriteLinePredicateType Singleton = new WriteLinePredicateType();

        private WriteLinePredicateType()
            : base("WriteLine", true, 1)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(input, constraint, context);
        }
    }
}
