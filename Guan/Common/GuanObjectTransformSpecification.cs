// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Guan.Common
{
    [DataContract]
    public class GuanObjectTransformSpecification
    {
        [DataContract]
        public class Rule
        {
            [DataMember]
            public string SourcePath;

            [DataMember]
            public string SourceExpression;

            [DataMember]
            public string Target;

            [DataMember]
            public bool IsRequired;

            public Rule()
            {
                IsRequired = true;
            }
        }

        class Tranformer : IGuanObjectTransformer
        {
            class StringWithVariable
            {
                private List<string> fragments_;
                private List<string> variables_;
                private bool literal_;

                private StringWithVariable(List<string> fragments, List<string> variables)
                {
                    fragments_ = fragments;
                    variables_ = variables;

                    literal_ = (fragments.Count == 2 && string.IsNullOrEmpty(fragments[0]) && string.IsNullOrEmpty(fragments[1]));
                }

                public string Evaluate(GuanObject obj)
                {
                    string result = fragments_[0];
                    for (int i = 1; i < fragments_.Count; i++)
                    {
                        object value = obj["__variable/" + variables_[i - 1]];
                        result = result + (value != null ? value.ToString() : "") + fragments_[i];
                    }

                    return result;
                }

                public object Evaluate(GuanObject original, GuanObject obj, IGuanExpressionContext expressionContext)
                {
                    if (literal_)
                    {
                        return obj["__variable/" + variables_[0]];
                    }

                    GuanExpression exp = GuanExpression.Build(Evaluate(obj), expressionContext);
                    return exp.Evaluate(original);
                }

                public static StringWithVariable Build(string original, List<string> variables)
                {
                    List<string> fragments = null;
                    List<string> names = null;

                    int last = 0;
                    int start = 0;
                    while (start >= 0)
                    {
                        start = original.IndexOf('?', start);
                        if (start < 0)
                        {
                            if (fragments == null)
                            {
                                return null;
                            }

                            fragments.Add(original.Substring(last));
                        }
                        else
                        {
                            string tmp = original.Substring(start + 1);
                            bool found = false;
                            foreach (string variable in variables)
                            {
                                if (tmp.StartsWith(variable))
                                {
                                    if (fragments == null)
                                    {
                                        fragments = new List<string>();
                                        names = new List<string>();
                                    }

                                    fragments.Add(original.Substring(last, start - last));
                                    names.Add(variable);
                                    start = last = start + variable.Length + 1;
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                start = start + 1;
                            }
                        }
                    }

                    return new StringWithVariable(fragments, names);
                }
            }

            class TransformRule
            {
                private Rule spec_;
                private IGuanExpressionContext expressionContext_;
                private string[] pathSegments_;
                private GuanExpression expression_;
                private GuanPredicate predicate_;
                private StringWithVariable expressionToEvaluate_;
                private StringWithVariable targetToEvaluate_;

                private static readonly Regex VariablePattern = new Regex(@"^?\d+$", RegexOptions.Compiled);

                public TransformRule(Rule spec, IGuanExpressionContext expressionContext, List<string> variables)
                {
                    spec_ = spec;
                    expressionContext_ = expressionContext;

                    if (!string.IsNullOrEmpty(spec_.SourcePath))
                    {
                        pathSegments_ = spec_.SourcePath.Trim('/').Split('/');

                        bool found = false;
                        foreach (string segment in pathSegments_)
                        {
                            if (VariablePattern.IsMatch(segment))
                            {
                                string name = segment.Substring(1);
                                if (!variables.Contains(name))
                                {
                                    variables.Add(name);
                                }
                                found = true;
                            }
                        }

                        if (!found)
                        {
                            pathSegments_ = null;
                        }
                    }

                    if (!string.IsNullOrEmpty(spec_.SourceExpression))
                    {
                        expressionToEvaluate_ = StringWithVariable.Build(spec_.SourceExpression, variables);
                        if (expressionToEvaluate_ == null)
                        {
                            if (string.IsNullOrEmpty(spec_.Target))
                            {
                                predicate_ = GuanPredicate.Build(spec_.SourceExpression, expressionContext_);
                            }
                            else
                            {
                                expression_ = GuanExpression.Build(spec_.SourceExpression, expressionContext_);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(spec_.Target))
                    {
                        targetToEvaluate_ = StringWithVariable.Build(spec_.Target, variables);
                    }
                }

                public void Process(GuanObject original, GuanObject target)
                {
                    object value;
                    if (string.IsNullOrEmpty(spec_.SourceExpression))
                    {
                        value = original[spec_.SourcePath];
                    }
                    else
                    {
                        value = expression_.Evaluate(original);
                    }

                    if (value != null)
                    {
                        target[spec_.Target] = value;
                    }
                }

                public void Process(GuanObject original, List<GuanObject> result)
                {
                    List<string> path = null;
                    if (pathSegments_ != null)
                    {
                        path = EvaluateVariables(original, result);
                    }

                    for (int i = result.Count - 1; i >= 0; i--)
                    {
                        if (!string.IsNullOrEmpty(spec_.Target))
                        {
                            object value;
                            if (string.IsNullOrEmpty(spec_.SourceExpression))
                            {
                                value = original[path != null ? path[i] : spec_.SourcePath];
                            }
                            else
                            {
                                value = EvaluateExpression(original, result[i]);
                            }

                            if (value != null)
                            {
                                result[i][EvaluateTarget(original)] = value;
                            }
                            else if (spec_.IsRequired)
                            {
                                result.RemoveAt(i);
                            }
                        }
                        else if (!EvaluatePredicate(original, result[i]))
                        {
                            result.RemoveAt(i);
                        }
                    }
                }

                private object EvaluateExpression(GuanObject original, GuanObject obj)
                {
                    GuanExpression expression = expression_;
                    if (expression == null)
                    {
                        return expressionToEvaluate_.Evaluate(original, obj, expressionContext_);
                    }

                    return expression.Evaluate(original);
                }

                private bool EvaluatePredicate(GuanObject original, GuanObject obj)
                {
                    GuanPredicate predicate = predicate_;
                    if (predicate == null)
                    {
                        predicate = GuanPredicate.Build(expressionToEvaluate_.Evaluate(obj), expressionContext_);
                    }

                    return predicate.Match(original);
                }

                private string EvaluateTarget(GuanObject obj)
                {
                    return (targetToEvaluate_ != null ? targetToEvaluate_.Evaluate(obj) : spec_.Target);
                }

                private List<string> EvaluateVariables(GuanObject original, List<GuanObject> result)
                {
                    List<string> path = new List<string>(result.Count);
                    for (int i = 0; i < pathSegments_.Length; i++)
                    {
                        string varName = (pathSegments_[i].StartsWith("?") ? "__variable/" + pathSegments_[i].Substring(1) : null);
                        int count = result.Count;
                        for (int j = 0; j < count; j++)
                        {
                            string segment;
                            if (varName == null)
                            {
                                segment = pathSegments_[i];
                            }
                            else
                            {
                                segment = (string)result[j][varName];
                                if (segment == null)
                                {
                                    GuanObject parent = original.GetObject(path[j]);
                                    if (parent != null)
                                    {
                                        foreach (var child in parent.Children)
                                        {
                                            GuanObject obj = new GuanObject(result[j]);
                                            obj[varName] = child.Key;
                                            result.Add(obj);
                                            path.Add(i == 0 ? child.Key : path[j] + "/" + child.Key);
                                            if (pathSegments_[i].StartsWith("??"))
                                            {
                                                break;
                                            }
                                        }
                                    }

                                    result.RemoveAt(j);
                                    path.RemoveAt(j);
                                    j--;
                                    count--;
                                    continue;
                                }
                            }

                            if (i == 0)
                            {
                                path.Add(segment);
                            }
                            else
                            {
                                path[j] = path[j] + "/" + segment;
                            }
                        }
                    }

                    return path;
                }
            }

            private List<TransformRule> rules_;
            private bool isSimple_;

            public Tranformer(GuanObjectTransformSpecification spec, IGuanExpressionContext expressionContext)
            {
                List<string> variables = new List<string>();

                rules_ = new List<TransformRule>(spec.Rules.Count);
                isSimple_ = true;

                foreach (var rule in spec.Rules)
                {
                    if (rule.Target == null)
                    {
                        isSimple_ = false;
                    }

                    rules_.Add(new TransformRule(rule, expressionContext, variables));
                }

                if (variables.Count > 0)
                {
                    isSimple_ = false;
                }
            }

            public List<GuanObject> Transform(GuanObject original)
            {
                List<GuanObject> result = new List<GuanObject>
                {
                    new GuanObject()
                };

                foreach (var rule in rules_)
                {
                    rule.Process(original, result);
                }

                foreach (GuanObject obj in result)
                {
                    obj.Delete("__variable");
                }

                return result;
            }

            public void Apply(GuanObject original, GuanObject target)
            {
                if (!isSimple_)
                {
                    throw new InvalidOperationException("Apply can only be called with simple transformer");
                }

                if (rules_.Count > 0)
                {
                    foreach (var rule in rules_)
                    {
                        rule.Process(original, target);
                    }
                }
                else if (original != target)
                {
                    target.CopyFrom(original);
                }
            }
        }

        [DataMember]
        public List<Rule> Rules;

        public GuanObjectTransformSpecification()
        {
            Rules = new List<Rule>();
        }

        public void AddPredicate(string predicate)
        {
            AddMapping(null, predicate, null);
        }

        public void AddMapping(string source)
        {
            AddMapping(source, source);
        }

        public void AddMapping(string source, string target)
        {
            AddMapping(source, null, target, true);
        }

        public void AddMapping(string sourcePath, string sourceExpression, string target, bool isRequired = true)
        {
            Rule rule = new Rule();
            rule.SourcePath = sourcePath;
            rule.SourceExpression = sourceExpression;
            rule.Target = target;
            rule.IsRequired = isRequired;
            Rules.Add(rule);
        }

        public IGuanObjectTransformer CreateTransformer(IGuanExpressionContext expressionContext)
        {
            return new Tranformer(this, expressionContext);
        }
    }
}
