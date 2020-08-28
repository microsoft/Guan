// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// Functor for compound term.
    /// </summary>
    public class Functor
    {
        private string name_;
        private List<KeyValuePair<string, ArgumentDescription>> args_;
        private List<string> requiredArguments_;

        private static readonly Regex NamePattern = new Regex(@"^[_\w]+$", RegexOptions.Compiled);
        private static readonly List<KeyValuePair<string, ArgumentDescription>> EmptyArgs = new List<KeyValuePair<string, ArgumentDescription>>();
        private static readonly List<string> EmptyRequiredArgs = new List<string>();

        public static readonly Functor ClassObject = new Functor("object");
        public static readonly Functor Empty = new Functor("");

        public Functor(string name)
        {
            name_ = name;
            args_ = EmptyArgs;
            requiredArguments_ = EmptyRequiredArgs;
        }

        public string Name
        {
            get
            {
                return name_;
            }
        }

        public ArgumentDescription GetArgumentDescription(string name)
        {
            foreach (var arg in args_)
            {
                if (arg.Key == name)
                {
                    return arg.Value;
                }
            }

            return null;
        }

        public List<string> RequiredArguments
        {
            get
            {
                return requiredArguments_;
            }
        }

        public void AddArgumentDescription(string name, ArgumentDescription arg)
        {
            if (GetArgumentDescription(name) != null)
            {
                throw new GuanException("Duplicate argument description for {0} in {1}", name, this);
            }

            if (args_ == EmptyArgs)
            {
                args_ = new List<KeyValuePair<string, ArgumentDescription>>();
            }

            args_.Add(new KeyValuePair<string, ArgumentDescription>(name, arg));

            if (arg.Required)
            {
                if (requiredArguments_ == EmptyRequiredArgs)
                {
                    requiredArguments_ = new List<string>();
                }
                requiredArguments_.Add(name);
            }
        }

        public virtual bool Unify(Functor other)
        {
            return (name_ == other.name_);
        }

        public override bool Equals(object obj)
        {
            Functor other = obj as Functor;
            return (other != null && name_ == other.name_);
        }

        public override int GetHashCode()
        {
            return name_.GetHashCode();
        }

        public override string ToString()
        {
            return name_;
        }

        internal static Functor Parse(string name)
        {
            if (PredicateType.GetBuiltInType(name) == null && !NamePattern.IsMatch(name))
            {
                throw new ArgumentException("Invalid functor name: " + name);
            }

            return new Functor(name);
        }
    }
}
