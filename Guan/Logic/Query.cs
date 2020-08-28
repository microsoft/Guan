using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// Class for a query on some pre-defined predicate type.
    /// </summary>
    public class Query
    {
        class QueryPredicateType : PredicateType
        {
            private CompoundTerm input_;

            public QueryPredicateType()
                : base(QueryTypeName)
            {
            }

            public CompoundTerm Input
            {
                get
                {
                    return input_;
                }
            }

            public override void AdjustTerm(CompoundTerm term, Rule rule)
            {
                ReleaseAssert.IsTrue(term == rule.Head && input_ == null);

                foreach (string name in rule.VariableTable)
                {
                    rule.AddArgument(term, "?" + name, name);
                }

                VariableBinding binding = rule.CreateBinding(0);
                input_ = term.DuplicateGoal(binding);
            }

            public override string ToString()
            {
                return string.Empty;
            }
        }

        class Provider : IFunctorProvider
        {
            private IFunctorProvider provider_;
            private QueryPredicateType type_;

            public Provider(IFunctorProvider provider, QueryPredicateType type)
            {
                provider_ = provider;
                type_ = type;
            }

            public Functor FindFunctor(string name, Module from)
            {
                if (name == QueryTypeName)
                {
                    return type_;
                }

                if (provider_ != null)
                {
                    return provider_.FindFunctor(name, from);
                }

                return null;
            }
        }

        private CompoundTerm input_;
        private PredicateResolver resolver_;

        private const string QueryTypeName = "__query";

        private Query(QueryPredicateType queryType, QueryContext queryContext)
        {
            input_ = queryType.Input;
            input_.Binding.MoveNext();
            resolver_ = queryType.CreateResolver(input_, Constraint.Empty, queryContext);
        }

        public async Task<List<Term>> GetResultsAsync(int maxCount)
        {
            List<Term> result = new List<Term>();
            while (result.Count < maxCount)
            {
                Term term = await GetNextAsync();
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
            if (resolver_.Iteration > 0)
            {
                input_.Binding.MovePrev();
            }

            UnificationResult result = await resolver_.GetNextAsync();
            if (result == null)
            {
                return null;
            }

            result.Apply(input_.Binding);
            input_.Binding.MoveNext();
            return input_;
        }

        public static Query Create(string text, QueryContext queryContext, IFunctorProvider provider)
        {
            if (text.Contains(":-"))
            {
                throw new ArgumentException("Query can't contain head: " + text);
            }

            QueryPredicateType queryType = new QueryPredicateType();

            List<string> types = new List<string>
            {
                QueryTypeName
            };
            List<string> rules = new List<string>
            {
                QueryTypeName + " :- " + text
            };

            _ = Module.Parse("query", rules, new Provider(provider, queryType), types);
            
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

            List<string> types = new List<string>
            {
                QueryTypeName
            };
            List<Rule> rules = new List<Rule>
            {
                Rule.Parse(ruleTerm, ruleTerm.ToString())
            };

            _ = Module.Parse("query", rules, new Provider(provider, queryType), types);
            
            return new Query(queryType, queryContext);
        }
    }
}
