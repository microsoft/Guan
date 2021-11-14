// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System.Threading.Tasks;

    /// <summary>
    /// Predicate type for updateobj.
    /// </summary>
    internal class UpdateObjectPredicateType : PredicateType
    {
        public static readonly UpdateObjectPredicateType Singleton = new UpdateObjectPredicateType();

        private UpdateObjectPredicateType()
            : base("updateobj", true, 3, 3)
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

            protected override Task<bool> CheckAsync()
            {
                Term term1 = this.GetInputArgument(0);
                IWritablePropertyContext context = term1.GetObjectValue() as IWritablePropertyContext;
                if (context == null)
                {
                    throw new GuanException("1st argument of updateobj {0} is not writable context", term1);
                }

                string name = this.GetInputArgumentString(1);
                context[name] = this.GetInputArgumentObject(2);

                return Task.FromResult(true);
            }
        }
    }
}
