//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Threading.Tasks;

    internal class SleepPredicateType : PredicateType
    {
        public static readonly SleepPredicateType Singleton = new SleepPredicateType();

        private SleepPredicateType()
            : base("sleep", true, 1)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(input, constraint, context);
        }

        private class Resolver : BooleanPredicateResolver
        {
            public Resolver(CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context)
            {
            }

            protected override async Task<bool> CheckAsync()
            {
                object value = this.GetInputArgumentObject(0);
                TimeSpan interval;
                if (value is TimeSpan)
                {
                    interval = (TimeSpan)value;
                }
                else if (value is long)
                {
                    interval = TimeSpan.FromSeconds((long)value);
                }
                else
                {
                    interval = TimeSpan.FromSeconds((double)value);
                }

                Console.WriteLine("Sleeping for {0}", interval);
                await Task.Delay(interval);
                Console.WriteLine("Sleep completed");

                return true;
            }
        }
    }
}
