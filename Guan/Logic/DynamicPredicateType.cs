///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.
//
// @File: DynamicPredicateType.cs
//
// @Owner: xunlu
// @Test:  xunlu
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
namespace Guan.Logic
{
    /// <summary>
    /// When a predicate type is marked as dynamic, when resolving the goal
    /// we will only look for asserted predicates.
    /// This is different from standard Prolog where the predicates are also
    /// getting resolved in standard ways.
    /// </summary>
    internal class DynamicPredicateType : PredicateType
    {
        public DynamicPredicateType(string name)
            : base(name)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            PredicateType assertedType = context.GetAssertedPredicateType(this.Name);
            if (assertedType == null)
            {
                assertedType = FailPredicateType.Singleton;
            }

            return assertedType.CreateResolver(input, constraint, context);
        }
    }
}
