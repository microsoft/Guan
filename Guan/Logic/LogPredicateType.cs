// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System.Threading.Tasks;

    /// <summary>
    /// Predicate type for writing a line to log.
    /// </summary>
    internal class LogPredicateType : PredicateType
    {
        public static readonly LogPredicateType LogInfo = new LogPredicateType("LogInfo");
        public static readonly LogPredicateType LogWarning = new LogPredicateType("LogWarning");
        public static readonly LogPredicateType LogError = new LogPredicateType("LogError");

        public LogPredicateType(string name)
            : base(name, true, 1)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(this, input, constraint, context);
        }

        private class Resolver : BooleanPredicateResolver
        {
            private LogPredicateType type;

            public Resolver(LogPredicateType type, CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
                this.type = type;
            }

            protected override Task<bool> CheckAsync()
            {
                string format = this.GetInputArgumentString(0);
                object[] args = new object[this.Input.Arguments.Count - 1];
                for (int i = 1; i < this.Input.Arguments.Count; i++)
                {
                    args[i - 1] = this.GetInputArgument(i);
                }

                if (this.type == LogError)
                {
                    EventLogWriter.WriteError(format, args);
                }
                else if (this.type == LogWarning)
                {
                    EventLogWriter.WriteWarning(format, args);
                }
                else
                {
                    EventLogWriter.WriteInfo(format, args);
                }

                return Task.FromResult(true);
            }
        }
    }
}
