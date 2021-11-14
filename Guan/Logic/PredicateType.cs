//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Functor for a predicate goal.
    /// </summary>
    public class PredicateType : Functor
    {
        private static object builtInTypesLock = new object();

        private static Dictionary<string, PredicateType> builtInTypes = null;

        private Module module;
        private bool isPublic;
        private int minPositionalArgument;
        private int maxPositionalArgument;
        private List<Rule> rules;
        private bool parallel;
        private bool singleActivation;
        private TermOption option;

        public PredicateType(string name, bool isPublic = false, int minPositionalArgument = 0, int maxPositionalArgument = int.MaxValue)
            : base(name)
        {
            this.isPublic = isPublic;
            this.minPositionalArgument = minPositionalArgument;
            this.maxPositionalArgument = maxPositionalArgument;
            this.parallel = false;
            this.singleActivation = false;
            this.option = TermOption.Default;
        }

        public bool IsPublic
        {
            get
            {
                return this.isPublic;
            }
        }

        public Module Module
        {
            get
            {
                return this.module;
            }
        }

        internal List<Rule> Rules
        {
            get
            {
                return this.rules;
            }
        }

        internal int MinPositionalArgument
        {
            get
            {
                return this.minPositionalArgument;
            }
        }

        internal int MaxPositionalArgument
        {
            get
            {
                return this.maxPositionalArgument;
            }
        }

        internal TermOption Option
        {
            get
            {
                return this.option;
            }
        }

        public static PredicateType GetBuiltInType(string name)
        {
            if (builtInTypes == null)
            {
                lock (builtInTypesLock)
                {
                    if (builtInTypes == null)
                    {
                        builtInTypes = CreateBuiltInTypes();
                    }
                }
            }

            PredicateType result;
            _ = builtInTypes.TryGetValue(name, out result);

            return result;
        }

        public virtual PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context)
        {
            if (this.rules == null)
            {
                return null;
            }

            if (this.parallel)
            {
                return new ParallelRulePredicateResolver(this.module, this.rules, input, constraint, context);
            }

            return new RulePredicateResolver(this.module, this.rules, this.singleActivation, input, constraint, context);
        }

        public virtual void AdjustTerm(CompoundTerm term, Rule rule)
        {
        }

        internal void LoadRules(List<string> rules, IFunctorProvider provider)
        {
            if (rules.Count == 0)
            {
                return;
            }

            List<string> publicTypes = new List<string>()
            {
                this.Name
            };

            Module module = Module.Parse(this.Name, rules, provider, publicTypes);
            ReleaseAssert.IsTrue(this.module == null || this.module == module);
            this.module = module;
        }

        internal void AddRule(Module module, Rule rule, bool append)
        {
            if (this.module != null && this.module != module)
            {
                throw new GuanException("Rules for {0} set from both modules {1} and {2}", this, this.module, module);
            }

            this.module = module;
            if (this.rules == null)
            {
                this.rules = new List<Rule>();
            }

            if (append)
            {
                this.rules.Add(rule);
            }
            else
            {
                this.rules.Insert(0, rule);
            }
        }

        internal void ProcessMetaData(string name, Term term)
        {
            if (name == null && term.GetStringValue() == "parallel")
            {
                this.parallel = true;
            }
            else if (name == null && term.GetStringValue() == "singleActivation")
            {
                this.singleActivation = true;
            }
            else
            {
                CompoundTerm compound = term as CompoundTerm;
                if (compound != null && compound.Functor.Name == "_option")
                {
                    this.option = new TermOption(compound);
                }
            }
        }

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
            AddType(result, WriteLinePredicateType.WriteInfo);
            AddType(result, WriteLinePredicateType.WriteWarning);
            AddType(result, WriteLinePredicateType.WriteError);
            AddType(result, LogPredicateType.LogInfo);
            AddType(result, LogPredicateType.LogWarning);
            AddType(result, LogPredicateType.LogError);
            AddType(result, SleepPredicateType.Singleton);
            AddType(result, RegexPredicateType.Singleton);
            AddType(result, QueryToListPredicateType.Singleton);

            return result;
        }

        private static void AddType(Dictionary<string, PredicateType> result, PredicateType type)
        {
            result.Add(type.Name, type);
        }
    }
}
