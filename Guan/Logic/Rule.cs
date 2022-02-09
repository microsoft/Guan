// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Rule which contains a head and an optional body.
    /// Parsing of a rule goes through the following steps:
    /// 1. Parse the TermExpression into compound term.
    /// 2. Break the compound term into head and bodies.
    /// 3. For head and each goal, resolve the corresponding predicate type
    /// 4. For every compound term, resolve the corresponding functor if there is one
    /// 5. Invoke PostProcessing on each head & goal, which includes handling
    ///    argument name and variables, plus type-specific posting processing.
    /// </summary>
    public class Rule
    {
        private static readonly Regex VariablePattern = new Regex(@"^\?[_\w]+$", RegexOptions.Compiled);
        private static readonly Regex ArgumentNamePattern = new Regex(@"^[_\.\w]+$", RegexOptions.Compiled);

        private string text;
        private CompoundTerm head;
        private List<CompoundTerm> goals;
        private VariableTable variableTable;

        internal Rule(string text, CompoundTerm head, List<CompoundTerm> goals, VariableTable variableTable)
        {
            this.text = text;
            this.head = head;
            this.goals = goals;
            this.variableTable = variableTable;
        }

        public CompoundTerm Head
        {
            get
            {
                return this.head;
            }
        }

        public List<CompoundTerm> Goals
        {
            get
            {
                return this.goals;
            }
        }

        internal VariableTable VariableTable
        {
            get
            {
                return this.variableTable;
            }
        }

        public static Rule Parse(string text)
        {
            return Parse(ToGoal(TermExpression.Parse(text)), text);
        }

        public void AddArgument(CompoundTerm term, string argument, string name)
        {
            Term arg = TermExpression.Parse(argument);
            term.AddArgument(arg, name);
            ProcessCompoundTerm(term, this.variableTable, 0);
        }

        public override string ToString()
        {
            return this.text;
        }

        internal static Rule Parse(CompoundTerm rule, string text)
        {
            CompoundTerm head = null;
            List<CompoundTerm> goals = new List<CompoundTerm>();

            if (rule != null)
            {
                if (rule.Functor.Name != ":-")
                {
                    head = rule;
                }
                else if (rule.Arguments.Count == 2)
                {
                    head = ToGoal(rule.Arguments[0].Value);
                    if (head == null)
                    {
                        throw new GuanException("Invalid head in rule {0}", text);
                    }

                    CompoundTerm body = ToGoal(rule.Arguments[1].Value);
                    if (body == null)
                    {
                        throw new GuanException("Invalid body in rule {0}", text);
                    }

                    ExpandBody(body, goals);
                }
            }

            if (head == null)
            {
                throw new GuanException("{0} is not a rule", text);
            }

            return new Rule(text, head, goals, new VariableTable());
        }

        internal void PostProcessing()
        {
            foreach (CompoundTerm goal in this.goals)
            {
                ProcessCompoundTerm(goal, this.variableTable, 0);
                goal.PostProcessing(this);
            }

            ProcessCompoundTerm(this.head, this.variableTable, 0);
            this.head.PostProcessing(this);
        }

        internal void ProcessMetaDataHead()
        {
            ProcessCompoundTerm(this.head, null, 0);
        }

        internal VariableBinding CreateBinding(int level)
        {
            return new VariableBinding(this.variableTable, this.goals.Count, level);
        }

        private static CompoundTerm ToGoal(Term term)
        {
            CompoundTerm result = term as CompoundTerm;
            if (result != null)
            {
                return result;
            }

            string name = term.GetStringValue();
            if (name == null)
            {
                return null;
            }

            if (VariablePattern.IsMatch(name))
            {
                result = new CompoundTerm(CallPredicateType.Singleton);
                result.AddArgument(term, "0");
                return result;
            }

            return new CompoundTerm(Functor.Parse(name));
        }

        private static void ExpandBody(CompoundTerm body, List<CompoundTerm> goals)
        {
            while (body.Functor.Name == ",")
            {
                ReleaseAssert.IsTrue(body.Arguments.Count == 2);
                CompoundTerm goal = ToGoal(body.Arguments[0].Value);
                CompoundTerm rest = ToGoal(body.Arguments[1].Value);
                if (goal == null || rest == null)
                {
                    throw new GuanException("Invalid goal {0}", body);
                }

                goals.Add(goal);
                body = rest;
            }

            goals.Add(body);
        }

        private static void ProcessCompoundTerm(CompoundTerm goal, VariableTable variableTable, int level)
        {
            if (level > 0 || goal.Functor.Name != "not")
            {
                level++;
            }

            PredicateType predicateType = goal.Functor as PredicateType;
            int minPositional = (predicateType != null ? predicateType.MinPositionalArgument : 0);
            int maxPositional = (predicateType != null ? predicateType.MaxPositionalArgument : int.MaxValue);
            bool positional = (maxPositional > 0);

            if (goal.Arguments.Count < minPositional)
            {
                throw new GuanException("Predicate {0} must have at leaat {1} argument(s)", goal, minPositional);
            }

            for (int i = 0; i < goal.Arguments.Count; i++)
            {
                bool nameOverride = false;
                CompoundTerm compound = goal.Arguments[i].Value as CompoundTerm;
                if (level > 0 && compound != null && compound.Functor.Name == "=")
                {
                    ReleaseAssert.IsTrue(compound.Arguments.Count == 2 && i >= minPositional);
                    string name = compound.Arguments[0].Value.GetStringValue();
                    if (name == null || !ArgumentNamePattern.IsMatch(name))
                    {
                        throw new GuanException("Invalid argument name {0} for {1}", name, compound.Arguments[1].Value);
                    }

                    positional = false;
                    goal.Arguments[i] = new TermArgument(name, compound.Arguments[1].Value, goal.Functor.GetArgumentDescription(name));
                    compound = goal.Arguments[i].Value as CompoundTerm;
                }
                else
                {
                    if (i >= maxPositional)
                    {
                        positional = false;
                    }

                    if (!positional)
                    {
                        nameOverride = true;
                    }
                }

                if (compound != null)
                {
                    if (nameOverride)
                    {
                        goal.Arguments[i] = new TermArgument(compound.Functor.Name, compound, goal.Functor.GetArgumentDescription(compound.Functor.Name));
                    }

                    ProcessCompoundTerm(compound, variableTable, level);

                    if (compound.Functor.Name == "[")
                    {
                        goal.Arguments[i].Value = ListTerm.Parse(compound);
                    }
                }
                else
                {
                    string constantValue = goal.Arguments[i].Value.GetStringValue();
                    string variableName;
                    if (constantValue == "_")
                    {
                        variableName = constantValue;
                    }
                    else if (constantValue != null && VariablePattern.IsMatch(constantValue))
                    {
                        variableName = constantValue.Substring(1);
                    }
                    else
                    {
                        variableName = null;
                    }

                    if (variableName != null)
                    {
                        int variableIndex = variableTable.GetIndex(variableName, true);
                        IndexedVariable variable = new IndexedVariable(variableIndex, variableName);
                        if (nameOverride)
                        {
                            goal.Arguments[i] = new TermArgument(variableName, variable, goal.Functor.GetArgumentDescription(variableName));
                        }
                        else
                        {
                            goal.Arguments[i].Value = variable;
                        }
                    }
                    else if (nameOverride)
                    {
                        goal.Arguments[i] = new TermArgument(constantValue, Constant.True, goal.Functor.GetArgumentDescription(constantValue));
                    }
                }
            }
        }
    }
}
