//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Class for a query on some pre-defined predicate type.
    /// </summary>
    public class Query
    {
        private const string QueryTypeName = "__query";

        private CompoundTerm input;
        private PredicateResolver resolver;

        private Query(QueryPredicateType queryType, QueryContext queryContext)
        {
            this.input = queryType.Input;
            this.input.Binding.MoveNext();
            this.resolver = queryType.CreateResolver(this.input, Constraint.Empty, queryContext);
        }

        public static Query Create(string text, QueryContext queryContext, IFunctorProvider provider)
        {
            if (text.Contains(":-"))
            {
                throw new ArgumentException("Query can't contain head: " + text);
            }

            QueryPredicateType queryType = new QueryPredicateType();

            List<string> types = new List<string>();
            types.Add(QueryTypeName);
            List<string> rules = new List<string>();
            rules.Add(QueryTypeName + " :- " + text);
            Module module = Module.Parse("query", rules, new Provider(provider, queryType), types);
            return new Query(queryType, queryContext);
        }

        public static Query Create(List<CompoundTerm> terms, QueryContext queryContext, IFunctorProvider provider)
        {
            if (terms == null || terms.Count == 0)
            {
                throw new ArgumentException("terms");
            }

            CompoundTerm body = terms[terms.Count - 1];
            for (int i = terms.Count - 2; i >= 0; i--)
            {
                CompoundTerm current = body;
                body = new CompoundTerm(",");
                body.AddArgument(terms[i], "0");
                body.AddArgument(current, "1");
            }

            QueryPredicateType queryType = new QueryPredicateType();

            CompoundTerm ruleTerm = new CompoundTerm(":-");
            ruleTerm.AddArgument(new CompoundTerm(QueryTypeName), "0");
            ruleTerm.AddArgument(body, "1");

            List<string> types = new List<string>()
            { 
                QueryTypeName
            };

            List<Rule> rules = new List<Rule>()
            {
                Rule.Parse(ruleTerm, ruleTerm.ToString())
            };

            _ = Module.Parse("query", rules, new Provider(provider, queryType), types);
            return new Query(queryType, queryContext);
        }

        public bool SetLocalVariables(string name, Term value)
        {
            return this.input.Binding.SetLocalVariableValue(name, value);
        }

        public async Task<List<Term>> GetResultsAsync(int maxCount)
        {
            List<Term> result = new List<Term>();
            while (result.Count < maxCount)
            {
                Term term = await this.GetNextAsync();
                if (term == null)
                {
                    return result;
                }

                result.Add(term.GetGroundedCopy());
            }

            return result;
        }

        public async Task<Term> GetNextAsync()
        {
            UnificationResult result = await this.GetNextResultAsync();
            if (result == null)
            {
                return null;
            }

            result.Apply(this.input.Binding);
            this.input.Binding.MoveNext();
            return this.input;
        }

        public Task<UnificationResult> GetNextResultAsync()
        {
            if (this.resolver.Iteration > 0)
            {
                _ = this.input.Binding.MovePrev();
            }

            return this.resolver.GetNextAsync();
        }

        private class QueryPredicateType : PredicateType
        {
            private CompoundTerm input;

            public QueryPredicateType()
                : base(QueryTypeName)
            {
            }

            public CompoundTerm Input
            {
                get
                {
                    return this.input;
                }
            }

            public override void AdjustTerm(CompoundTerm term, Rule rule)
            {
                ReleaseAssert.IsTrue(term == rule.Head && this.input == null);

                foreach (string name in rule.VariableTable)
                {
                    rule.AddArgument(term, "?" + name, name);
                }

                VariableBinding binding = rule.CreateBinding(0);
                this.input = term.DuplicateGoal(binding);
            }

            public override string ToString()
            {
                return string.Empty;
            }
        }

        private class Provider : IFunctorProvider
        {
            private IFunctorProvider provider;
            private QueryPredicateType type;

            public Provider(IFunctorProvider provider, QueryPredicateType type)
            {
                this.provider = provider;
                this.type = type;
            }

            public Functor FindFunctor(string name, Module from)
            {
                if (name == QueryTypeName)
                {
                    return this.type;
                }

                if (this.provider != null)
                {
                    return this.provider.FindFunctor(name, from);
                }

                return null;
            }
        }
    }
}
