// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Predicate type for writing a line to log.
    /// </summary>
    internal class WriteLinePredicateType : PredicateType
    {
        public static readonly WriteLinePredicateType WriteInfo = new WriteLinePredicateType("WriteInfo");
        public static readonly WriteLinePredicateType WriteWarning = new WriteLinePredicateType("WriteWarning");
        public static readonly WriteLinePredicateType WriteError = new WriteLinePredicateType("WriteError");

        private WriteLinePredicateType(string name)
            : base(name, true, 1)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(this, input, constraint, context);
        }

        private class Resolver : BooleanPredicateResolver
        {
            private WriteLinePredicateType type;

            public Resolver(WriteLinePredicateType type, CompoundTerm input, Constraint constraint, QueryContext context)
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

                if (this.type == WriteError)
                {
                    ConsoleSink.WriteLine(ConsoleColor.Red, format, args);
                }
                else if (this.type == WriteWarning)
                {
                    ConsoleSink.WriteLine(ConsoleColor.Yellow, format, args);
                }
                else
                {
                    Console.WriteLine(format, args);
                }

                return Task.FromResult(true);
            }
        }
    }
}
