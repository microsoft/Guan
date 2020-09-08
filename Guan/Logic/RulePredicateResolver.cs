// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// Resolver using rules. This is typical case except for external predicates.
    /// </summary>
    internal class RulePredicateResolver : PredicateResolver
    {
        private Module module_;
        private List<Rule> rules_;
        private bool singleActivation_;
        private Rule currentRule_;
        private int ruleIndex_;
        private VariableBinding binding_;
        private VariableBinding tail_;
        private PredicateResolver[] resolvers_;
        private Constraint[] constraints_;
        private List<Task<UnificationResult>> tasks_;

        public RulePredicateResolver(Module module, List<Rule> rules, bool singleActivation, CompoundTerm input, Constraint constraint, QueryContext context)
            : base(input, constraint, context)
        {
            module_ = module;
            rules_ = rules;
            singleActivation_ = singleActivation;
            ruleIndex_ = 0;
        }

        public override async Task<UnificationResult> OnGetNextAsync()
        {
            if (binding_ != null)
            {
                // when tail optimization is effective, both binding need to backtrack.
                if (tail_ != null)
                {
                    tail_.MovePrev();
                }

                Backtrack();
            }

            while (ruleIndex_ < rules_.Count)
            {
                if (binding_ == null)
                {
                    if (InitializeRule())
                    {
                        binding_.MoveNext();
                    }
                    else
                    {
                        ruleIndex_++;
                    }
                }
                else
                {
                    int currentIndex = binding_.CurrentIndex;
                    if (currentIndex == currentRule_.Goals.Count)
                    {
                        UnificationResult result = binding_.CreateOutput();
                        // When tail optimization is effective, the output is a two-stage
                        // process: we use the current binding output to update tail_ and
                        // then the real output is from _tail.
                        if (tail_ != null)
                        {
                            result.Apply(tail_);
                            tail_.MoveNext();
                            WriteTrace("T-Exit");
                            result = tail_.CreateOutput();
                        }

                        return result;
                    }

                    if (resolvers_[currentIndex] == null)
                    {
                        resolvers_[currentIndex] = CreateResolver(currentIndex);
                        if (currentRule_.Goals[currentIndex].PredicateType is CutPredicateType)
                        {
                            for (int i = currentIndex - 1; i >= 0; i--)
                            {
                                resolvers_[i].Cancel();
                            }
                        }

                        if (currentIndex == currentRule_.Goals.Count - 1 && !Context.EnableDebug && OptimizeTail())
                        {
                            WriteTrace("T-Call");
                            continue;
                        }
                    }

                    resolvers_[currentIndex].WriteTrace("Call");

                    UnificationResult goalResult;
                    if (tasks_ == null || tasks_.Count == 0)
                    {
                        goalResult = await resolvers_[currentIndex].GetNextAsync();
                    }
                    else
                    {
                        // When backtracked to a goal (so that its iteration is not 0),
                        // if it is a goal immediately before a forward cut, the task for
                        // this goal is already initiated and in the tasks_ collection.
                        if ((currentIndex + 1 == currentRule_.Goals.Count) ||
                            (resolvers_[currentIndex].Iteration == 0) ||
                            !(currentRule_.Goals[currentIndex + 1].PredicateType is ForwardCutPredicateType))
                        {
                            tasks_.Add(resolvers_[currentIndex].GetNextAsync());
                        }

                        while (true)
                        {
                            Task<UnificationResult> completedTask = await Task.WhenAny(tasks_);
                            goalResult = completedTask.Result;
                            if (completedTask == tasks_[tasks_.Count - 1])
                            {
                                tasks_.RemoveAt(tasks_.Count - 1);
                                break;
                            }

                            if (goalResult != null)
                            {
                                // If an earlier parallel task is completed, cancel all tasks after
                                // it.
                                int index = tasks_.IndexOf(completedTask);
                                int count = tasks_.Count - index;
                                for (; currentIndex > 0 && count > 1; currentIndex--)
                                {
                                    resolvers_[currentIndex].Context.IsCancelled = true;
                                    resolvers_[currentIndex] = null;

                                    if (currentRule_.Goals[currentIndex].PredicateType is ForwardCutPredicateType)
                                    {
                                        count--;
                                    }
                                }

                                ReleaseAssert.IsTrue(count == 1);

                                tasks_.RemoveRange(index, tasks_.Count - index);
                                binding_ = resolvers_[currentIndex].Input.Binding;

                                break;
                            }
                            tasks_.Remove(completedTask);
                        }
                    }

                    if (goalResult != null)
                    {
                        goalResult.Apply(binding_);
                        bool proceed = UpdateConstraints(goalResult);
                        binding_.MoveNext();

                        if (proceed)
                        {
                            resolvers_[currentIndex].WriteTrace("Exit");
                        }
                        else
                        {
                            Backtrack();
                        }
                    }
                    else if (currentRule_.Goals[currentIndex].PredicateType is ForwardCutPredicateType)
                    {
                        resolvers_[currentIndex] = null;
                        currentIndex--;
                        binding_ = resolvers_[currentIndex].Input.Binding;
                        resolvers_[currentIndex].Context.ClearChildren();
                    }
                    else
                    {
                        resolvers_[currentIndex].WriteTrace("Fail");
                        Backtrack();
                    }
                }
            }

            return null;
        }

        private bool InitializeRule()
        {
            currentRule_ = rules_[ruleIndex_];
            binding_ = currentRule_.CreateBinding(Input.Binding.Level + 1);
            if (!binding_.Unify(currentRule_.Head.DuplicateGoal(binding_), Input))
            {
                binding_ = null;
                return false;
            }

            resolvers_ = new PredicateResolver[currentRule_.Goals.Count];
            constraints_ = new Constraint[currentRule_.Goals.Count];
            if (currentRule_.Goals.Count > 0)
            {
                constraints_[0] = Constraint;
            }

            return true;
        }

        private PredicateResolver CreateResolver(int index)
        {
            CompoundTerm goal = currentRule_.Goals[index];

            QueryContext context = null;
            for (int i = index - 1; i >= 0 && context == null; i--)
            {
                context = resolvers_[i].Context;
            }

            if (context == null)
            {
                context = Context;
            }

            // For forwardcut, the context will be a child to the context for the goal
            // immediately before it.
            if (currentRule_.Goals[index].PredicateType is ForwardCutPredicateType && index > 0 && !resolvers_[index - 1].Completed)
            {
                if (tasks_ == null)
                {
                    tasks_ = new List<Task<UnificationResult>>();
                }

                VariableBinding currentBinding = binding_;
                binding_ = new VariableBinding(binding_);
                currentBinding.MovePrev();
                context = context.CreateChild();
                tasks_.Add(resolvers_[index - 1].GetNextAsync());
            }

            PredicateResolver result = goal.PredicateType.CreateResolver(goal.DuplicateGoal(binding_), constraints_[index], context);
            if (result == null)
            {
                throw new GuanException("No resolver defined for {0}", goal.PredicateType);
            }

            return result;
        }

        private bool UpdateConstraints(UnificationResult result)
        {
            int currentIndex = binding_.CurrentIndex;
            Constraint current = constraints_[currentIndex];
            Constraint next;

            // The constraints propagated to the next goal comes from two sources:
            // One is the constraints from the current goal (the ones remaining after
            // applying the output from the current goal, second is the constraints
            // added by the current goal.
            if (currentIndex == currentRule_.Goals.Count - 1)
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
                    Constant evaluted = current.Terms[i].Evaluate(Context) as Constant;
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
                constraints_[currentIndex + 1] = next;
            }

            return true;
        }

        private void Backtrack()
        {
            int currentIndex = binding_.CurrentIndex;
            if (currentIndex < resolvers_.Length && resolvers_[currentIndex] != null)
            {
                resolvers_[currentIndex].OnBacktrack();
                resolvers_[currentIndex] = null;
            }

            if (binding_.MovePrev())
            {
                if (currentRule_.Goals[currentIndex - 1].Functor is CutPredicateType)
                {
                    ruleIndex_ = rules_.Count;
                }
            }
            else if (singleActivation_ && Iteration > 0)
            {
                ruleIndex_ = rules_.Count;
            }
            else
            {
                ruleIndex_++;
                binding_ = null;
            }
        }

        private bool OptimizeTail()
        {
            int index = currentRule_.Goals.Count - 1;
            RulePredicateResolver ruleResolver = resolvers_[index] as RulePredicateResolver;
            // We can optimize if the goal is defined by rule and it does not require us
            // to maintain different Max count.
            if (ruleResolver == null || ruleResolver.Max != Max)
            {
                return false;
            }

            // TODO: allow optimization when there is constraint
            if (constraints_[index].Terms.Count > 0)
            {
                return false;
            }

            // Find the previous incomplete goal or cut.
            bool cut = false;
            bool incomplete = false;
            for (int i = index - 1; i >= 0 && !cut && !incomplete; i--)
            {
                if (currentRule_.Goals[i].Functor == CutPredicateType.Singleton)
                {
                    cut = true;
                }
                else if (!resolvers_[i].Completed)
                {
                    incomplete = true;
                }
            }

            // Optimize if cut or this is the last goal for the predicate
            bool result = (cut || (!incomplete && ruleIndex_ == rules_.Count - 1));
            if (result)
            {
                UpdateTail(ruleResolver);
                Load(ruleResolver);
            }

            return result;
        }

        private void UpdateTail(RulePredicateResolver resolver)
        {
            if (tail_ == null)
            {
                tail_ = binding_;
            }
            else
            {
                // Use whatever output we have currently to update _tail
                UnificationResult result = binding_.CreateOutput();
                result.Apply(tail_);
                // The input to the next goal is for the current bining, we need t
                // update so that it uses tail_.
                resolver.Input.Migrate(tail_);
            }

            tail_.ResetTail();
            binding_ = null;
        }

        private void Load(RulePredicateResolver resolver)
        {
            Input = resolver.Input;
            Iteration = 0;
            module_ = resolver.module_;
            rules_ = resolver.rules_;
            ruleIndex_ = 0;
        }
    }
}
