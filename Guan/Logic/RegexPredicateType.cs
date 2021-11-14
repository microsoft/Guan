//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    /// <summary>
    /// Predicate type for getval.
    /// </summary>
    internal class RegexPredicateType : PredicateType
    {
        public static readonly RegexPredicateType Singleton = new RegexPredicateType();

        private RegexPredicateType()
            : base("regex", true, 2, 2)
        {
        }

        public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            return new Resolver(input, constraint, context);
        }

        private class Resolver : GroundPredicateResolver
        {
            public Resolver(CompoundTerm input, Constraint constraint, QueryContext context)
                : base(input, constraint, context, 1)
            {
            }

            protected override Task<Term> GetNextTermAsync()
            {
                string text = this.GetInputArgumentString(0);
                Regex pattern = new Regex(this.GetInputArgumentString(1));

                CompoundTerm result;
                Match match = pattern.Match(text);
                if (match.Success)
                {
                    result = new CompoundTerm(this.Input.Functor, null);
                    for (int i = 2; i < this.Input.Arguments.Count; i++)
                    {
                        string name = this.Input.Arguments[i].Name;
                        result.AddArgument(new Constant(match.Groups[name].Value), name);
                    }
                }
                else
                {
                    result = null;
                }

                return Task.FromResult<Term>(result);
            }
        }
    }
}
