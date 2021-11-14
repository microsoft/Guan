//---------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Resolver using rules. This is typical case except for external predicates.
    /// </summary>
    internal class RulePredicateResolver : PredicateResolver
    {
        private Module module;
        private List<Rule> rules;
        private bool singleActivation;
        private Rule currentRule;
        private int ruleIndex;
        private VariableBinding binding;
        private VariableBinding tail;
        private PredicateResolver[] resolvers;
        private Constraint[] constraints;
        private List<Task<UnificationResult>> tasks;

        public RulePredicateResolver(Module module, List<Rule> rules, bool singleActivation, CompoundTerm input, Constraint constraint, QueryContext context)
            : base(input, constraint, context)
        {
            this.module = module;
            this.rules = rules;
            this.singleActivation = singleActivation;
            this.ruleIndex = 0;
        }

        public override async Task<UnificationResult> OnGetNextAsync()
        {
            if (this.binding != null)
            {
                // when tail optimization is effective, both binding need to backtrack.
                if (this.tail != null)
                {
                    _ = this.tail.MovePrev();
                }

                this.Backtrack();
            }

            while (this.ruleIndex < this.rules.Count)
            {
                if (this.binding == null)
                {
                    if (this.InitializeRule())
                    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        this.binding.MoveNext();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }
                    else
                    {
                        this.ruleIndex++;
                    }
                }
                else
                {
                    int currentIndex = this.binding.CurrentIndex;
                    if (currentIndex == this.currentRule.Goals.Count)
                    {
                        UnificationResult result = this.binding.CreateOutput();
                        // When tail optimization is effective, the output is a two-stage
                        // process: we use the current binding output to update tail_ and
                        // then the real output is from _tail.
                        if (this.tail != null)
                        {
                            result.Apply(this.tail);
                            this.tail.MoveNext();
                            this.WriteTrace("T-Exit");
                            result = this.tail.CreateOutput();
                        }

                        if (!this.Completed && this.IsCompleted(this.currentRule.Goals.Count - 1))
                        {
                            this.Complete();
                        }

                        return result;
                    }

                    if (this.resolvers[currentIndex] == null)
                    {
                        this.resolvers[currentIndex] = this.CreateResolver(currentIndex);
                        if (this.currentRule.Goals[currentIndex].PredicateType is CutPredicateType)
                        {
                            for (int i = currentIndex - 1; i >= 0; i--)
                            {
                                this.resolvers[i].Cancel();
                            }
                        }

                        if (currentIndex == this.currentRule.Goals.Count - 1 && !this.Context.EnableDebug && this.OptimizeTail())
                        {
                            this.WriteTrace("T-Call");
                            continue;
                        }
                    }

                    this.resolvers[currentIndex].WriteTrace("Call");

                    UnificationResult goalResult;
                    if (this.tasks == null || this.tasks.Count == 0)
                    {
                        goalResult = await this.resolvers[currentIndex].GetNextAsync();
                    }
                    else
                    {
                        // When backtracked to a goal (so that its iteration is not 0),
                        // if it is a goal immediately before a forward cut, the task for
                        // this goal is already initiated and in the tasks_ collection.
                        if ((currentIndex + 1 == this.currentRule.Goals.Count) ||
                            (this.resolvers[currentIndex].Iteration == 0) ||
                            !(this.currentRule.Goals[currentIndex + 1].PredicateType is ForwardCutPredicateType))
                        {
                            this.tasks.Add(this.resolvers[currentIndex].GetNextAsync());
                        }

                        while (true)
                        {
                            Task<UnificationResult> completedTask = await Task.WhenAny(this.tasks);
                            goalResult = await completedTask;
                            if (completedTask == this.tasks[this.tasks.Count - 1])
                            {
                                this.tasks.RemoveAt(this.tasks.Count - 1);
                                break;
                            }
                            else if (goalResult != null)
                            {
                                // If an earlier parallel task is completed, cancel all tasks after
                                // it.
                                int index = this.tasks.IndexOf(completedTask);
                                int count = this.tasks.Count - index;
                                for (; currentIndex > 0 && count > 1; currentIndex--)
                                {
                                    this.resolvers[currentIndex].Context.IsCancelled = true;
                                    this.resolvers[currentIndex] = null;

                                    if (this.currentRule.Goals[currentIndex].PredicateType is ForwardCutPredicateType)
                                    {
                                        count--;
                                    }
                                }

                                ReleaseAssert.IsTrue(count == 1);

                                this.tasks.RemoveRange(index, this.tasks.Count - index);
                                this.binding = this.resolvers[currentIndex].Input.Binding;

                                break;
                            }
                            else
                            {
                                _ = this.tasks.Remove(completedTask);
                            }
                        }
                    }

                    if (goalResult != null)
                    {
                        goalResult.Apply(this.binding);
                        bool proceed = this.UpdateConstraints(goalResult);
                        this.binding.MoveNext();

                        if (proceed)
                        {
                            this.resolvers[currentIndex].WriteTrace("Exit");
                        }
                        else
                        {
                            this.Backtrack();
                        }
                    }
                    else if (this.currentRule.Goals[currentIndex].PredicateType is ForwardCutPredicateType)
                    {
                        this.resolvers[currentIndex] = null;
                        currentIndex--;
                        this.binding = this.resolvers[currentIndex].Input.Binding;
                        this.resolvers[currentIndex].Context.ClearChildren();
                    }
                    else
                    {
                        this.resolvers[currentIndex].WriteTrace("Fail");
                        this.Backtrack();
                    }
                }
            }

            return null;
        }

        private bool InitializeRule()
        {
            this.currentRule = this.rules[this.ruleIndex];
            this.binding = this.currentRule.CreateBinding(this.Input.Binding.Level + 1);
            if (!this.binding.Unify(this.currentRule.Head.DuplicateGoal(this.binding), this.Input))
            {
                this.binding = null;
                return false;
            }

            this.resolvers = new PredicateResolver[this.currentRule.Goals.Count];
            this.constraints = new Constraint[this.currentRule.Goals.Count];
            if (this.currentRule.Goals.Count > 0)
            {
                this.constraints[0] = this.Constraint;
            }

            return true;
        }

        private PredicateResolver CreateResolver(int index)
        {
            CompoundTerm goal = this.currentRule.Goals[index];

            QueryContext context = null;
            for (int i = index - 1; i >= 0 && context == null; i--)
            {
                context = this.resolvers[i].Context;
            }

            if (context == null)
            {
                context = this.Context;
            }

            // For forwardcut, the context will be a child to the context for the goal
            // immediately before it.
            if (this.currentRule.Goals[index].PredicateType is ForwardCutPredicateType && index > 0 && !this.resolvers[index - 1].Completed)
            {
                if (this.tasks == null)
                {
                    this.tasks = new List<Task<UnificationResult>>();
                }

                VariableBinding currentBinding = this.binding;
                this.binding = new VariableBinding(this.binding);
                _ = currentBinding.MovePrev();
                context = context.CreateChild();
                this.tasks.Add(this.resolvers[index - 1].GetNextAsync());
            }

            PredicateResolver result = goal.PredicateType.CreateResolver(goal.DuplicateGoal(this.binding), this.constraints[index], context);
            if (result == null)
            {
                throw new GuanException("No resolver defined for {0}", goal.PredicateType);
            }

            return result;
        }

        private bool UpdateConstraints(UnificationResult result)
        {
            int currentIndex = this.binding.CurrentIndex;
            Constraint current = this.constraints[currentIndex];
            Constraint next;

            // The constraints propagated to the next goal comes from two sources:
            // One is the constraints from the current goal (the ones remaining after
            // applying the output from the current goal, second is the constraints
            // added by the current goal.
            if (currentIndex == this.currentRule.Goals.Count - 1)
            {
                next = null;
            }
            else if (result.Constraints != null)
            {
                next = new Constraint();
                next.Add(result.Constraints);
            }
            else
            {
                next = current;
            }

            // At this point, next is either the same as currennt, or contains nothing
            // from current.
            if (!result.IsEmpty)
            {
                for (int i = 0; i < current.Terms.Count; i++)
                {
                    Constant evaluted = current.Terms[i].Evaluate(this.Context) as Constant;
                    if (evaluted == null)
                    {
                        if (next != current && next != null)
                        {
                            next.Add(current.Terms[i]);
                        }
                    }
                    else if (!evaluted.IsTrue())
                    {
                        return false;
                    }
                    else if (next == current)
                    {
                        // Add previous constraints that have verified to remain pending,
                        // which effectlively removes the current constraint
                        next = new Constraint();
                        for (int j = 0; j < i; j++)
                        {
                            next.Add(current.Terms[j]);
                        }
                    }
                }
            }
            else if (next != current && next != null)
            {
                next.Add(current.Terms);
            }

            if (next != null)
            {
                this.constraints[currentIndex + 1] = next;
            }

            return true;
        }

        private void Backtrack()
        {
            int currentIndex = this.binding.CurrentIndex;
            if (currentIndex < this.resolvers.Length && this.resolvers[currentIndex] != null)
            {
                this.resolvers[currentIndex].OnBacktrack();
                this.resolvers[currentIndex] = null;
            }

            if (this.binding.MovePrev())
            {
                if (this.currentRule.Goals[currentIndex - 1].Functor is CutPredicateType)
                {
                    this.ruleIndex = this.rules.Count;
                }
            }
            else if (this.singleActivation && this.Iteration > 0)
            {
                this.ruleIndex = this.rules.Count;
            }
            else
            {
                this.ruleIndex++;
                this.binding = null;
            }
        }

        private bool OptimizeTail()
        {
            if (this.Input.Option.IsTraceEnabled(null).Item1)
            {
                return false;
            }

            int index = this.currentRule.Goals.Count - 1;
            RulePredicateResolver ruleResolver = this.resolvers[index] as RulePredicateResolver;
            // We can optimize if the goal is defined by rule and it does not require us
            // to maintain different Max count.
            if (ruleResolver == null || ruleResolver.Max != this.Max)
            {
                return false;
            }

            // TODO: allow optimization when there is constraint
            if (this.constraints[index].Terms.Count > 0)
            {
                return false;
            }

            if (!this.IsCompleted(index - 1))
            {
                return false;
            }

            this.UpdateTail(ruleResolver);
            this.Load(ruleResolver);

            return true;
        }

        private bool IsCompleted(int index)
        {
            for (int i = index; i >= 0; i--)
            {
                if (this.currentRule.Goals[i].Functor == CutPredicateType.Singleton)
                {
                    return true;
                }
                else if (!this.resolvers[i].Completed)
                {
                    return false;
                }
            }

            return (this.ruleIndex == this.rules.Count - 1);
        }

        private void UpdateTail(RulePredicateResolver resolver)
        {
            if (this.tail == null)
            {
                this.tail = this.binding;
            }
            else
            {
                // Use whatever output we have currently to update _tail
                UnificationResult result = this.binding.CreateOutput();
                result.Apply(this.tail);
                // The input to the next goal is for the current bining, we need t
                // update so that it uses tail_.
                resolver.Input.Migrate(this.tail);
            }

            this.tail.ResetTail();
            this.binding = null;
        }

        private void Load(RulePredicateResolver resolver)
        {
            this.Input = resolver.Input;
            this.Iteration = 0;
            this.module = resolver.module;
            this.rules = resolver.rules;
            this.ruleIndex = 0;
        }
    }
}
