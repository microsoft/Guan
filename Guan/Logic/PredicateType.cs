// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// Functor for a predicate goal.
    /// </summary>
    public class PredicateType : Functor
    {
        private Module module_;
        private bool isPublic_;
        private int minArgument_;
        private int maxArgument_;
        private List<Rule> rules_;
        private bool parallel_;
        private bool singleActivation_;

        private static Dictionary<string, PredicateType> BuiltInTypes = null;

        private static Dictionary<string, PredicateType> CreateBuiltInTypes()
        {
            Dictionary<string, PredicateType> result = new Dictionary<string, PredicateType>();
            AddType(result, CutPredicateType.Singleton);
            AddType(result, ForwardCutPredicateType.Singleton);
            AddType(result, FailPredicateType.Singleton);
            AddType(result, UnifyPredicateType.Regular);
            AddType(result, NotPredicateType.Singleton);
            AddType(result, TermPropertyPredicateType.Var);
            AddType(result, TermPropertyPredicateType.NonVar);
            AddType(result, TermPropertyPredicateType.Atom);
            AddType(result, TermPropertyPredicateType.Compound);
            AddType(result, TermPropertyPredicateType.Ground);
            AddType(result, TracePredicateType.Enable);
            AddType(result, TracePredicateType.Disable);
            AddType(result, GetValPredicateType.Singleton);
            AddType(result, SetValPredicateType.Backtrack);
            AddType(result, SetValPredicateType.NoBacktrack);
            AddType(result, EnumerablePredicateType.Singleton);
            AddType(result, AssertPredicateType.Assert);
            AddType(result, AssertPredicateType.Asserta);
            AddType(result, AssertPredicateType.Assertz);
            AddType(result, UpdateObjectPredicateType.Singleton);
            AddType(result, IsPredicateType.Singleton);
            AddType(result, WriteLinePredicateType.Singleton);

            return result;
        }

        private static void AddType(Dictionary<string, PredicateType> result, PredicateType type)
        {
            result.Add(type.Name, type);
        }

        public PredicateType(string name, bool isPublic = false, int minArgument = 0, int maxArgument = int.MaxValue)
            : base(name)
        {
            isPublic_ = isPublic;
            minArgument_ = minArgument;
            maxArgument_ = maxArgument;
            parallel_ = false;
            singleActivation_ = false;
        }

        public bool IsPublic
        {
            get
            {
                return isPublic_;
            }
        }

        public Module Module
        {
            get
            {
                return module_;
            }
        }

        internal List<Rule> Rules
        {
            get
            {
                return rules_;
            }
        }

        internal bool AllowPositionalArgument
        {
            get
            {
                return minArgument_ >= 0;
            }
        }

        internal void LoadRules(List<string> rules, IFunctorProvider provider)
        {
            if (rules.Count == 0)
            {
                return;
            }

            List<string> publicTypes = new List<string>
            {
                Name
            };

            Module module = Module.Parse(Name, rules, provider, publicTypes);
            ReleaseAssert.IsTrue(module_ == null || module_ == module);
            module_ = module;
        }

        internal void AddRule(Module module, Rule rule, bool append)
        {
            if (module_ != null && module_ != module)
            {
                throw new GuanException("Rules for {0} set from both modules {1} and {2}", this, module_, module);
            }

            module_ = module;
            if (rules_ == null)
            {
                rules_ = new List<Rule>();
            }

            if (append)
            {
                rules_.Add(rule);
            }
            else
            {
                rules_.Insert(0, rule);
            }
        }

        public virtual PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            if (rules_ == null)
            {
                return null;
            }

            if (parallel_)
            {
                return new ParallelRulePredicateResolver(module_, rules_, input, constraint, context);
            }

            return new RulePredicateResolver(module_, rules_, singleActivation_, input, constraint, context);
        }

        public virtual void AdjustTerm(CompoundTerm term, Rule rule)
        {
            if (minArgument_ >= 0 && term.Arguments.Count < minArgument_)
            {
                throw new GuanException("Predicate must have at least {0} argument(s)", minArgument_);
            }

            if (maxArgument_ >= 0 && term.Arguments.Count > maxArgument_)
            {
                throw new GuanException("Predicate can have at most {0} argument(s)", maxArgument_);
            }
        }

        internal void ProcessMetaData(string name, Term term)
        {
            if (name == null && term.GetStringValue() == "parallel")
            {
                parallel_ = true;
            }
            else if (name == null && term.GetStringValue() == "singleActivation")
            {
                singleActivation_ = true;
            }
        }

        public static PredicateType GetBuiltInType(string name)
        {
            if (BuiltInTypes == null)
            {
                lock (typeof(PredicateType))
                {
                    if (BuiltInTypes == null)
                    {
                        BuiltInTypes = CreateBuiltInTypes();
                    }
                }
            }

            PredicateType result;
            BuiltInTypes.TryGetValue(name, out result);

            return result;
        }
    }

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
            PredicateType assertedType = context.GetAssertedPredicateType(Name);
            if (assertedType == null)
            {
                assertedType = FailPredicateType.Singleton;
            }

            return assertedType.CreateResolver(input, constraint, context);
        }
    }
}
