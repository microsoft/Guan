// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Functor for compound term.
    /// </summary>
    public class Functor
    {
        public static readonly Functor ClassObject = new Functor("object");
        public static readonly Functor Empty = new Functor(string.Empty);

        private static readonly Regex NamePattern = new Regex(@"^[_\w]+$", RegexOptions.Compiled);
        private static readonly List<string> EmptyRequiredArgs = new List<string>();

        private string name;
        private Type unificationType;
        private List<KeyValuePair<string, ArgumentDescription>> args;
        private List<string> requiredArguments;

        public Functor(string name)
        {
            this.name = name;
        }

        internal Functor(Type type)
        {
            this.name = type.Name;
            this.unificationType = type;
        }

        public string Name
        {
            get
            {
                return this.name;
            }
        }

        public List<string> RequiredArguments
        {
            get
            {
                return this.requiredArguments ?? EmptyRequiredArgs;
            }
        }

        internal Type UnificationType
        {
            get
            {
                return this.unificationType;
            }
        }

        public ArgumentDescription GetArgumentDescription(string name)
        {
            if (this.args == null)
            {
                return null;
            }

            foreach (KeyValuePair<string, ArgumentDescription> arg in this.args)
            {
                if (arg.Key == name)
                {
                    return arg.Value;
                }
            }

            return null;
        }

        public void AddArgumentDescription(string name, ArgumentDescription arg)
        {
            if (this.GetArgumentDescription(name) != null)
            {
                throw new GuanException("Duplicate argument description for {0} in {1}", name, this);
            }

            if (this.args == null)
            {
                this.args = new List<KeyValuePair<string, ArgumentDescription>>();
            }

            this.args.Add(new KeyValuePair<string, ArgumentDescription>(name, arg));

            if (arg.Required)
            {
                if (this.requiredArguments == null)
                {
                    this.requiredArguments = new List<string>();
                }

                this.requiredArguments.Add(name);
            }
        }

        public override bool Equals(object obj)
        {
            Functor other = obj as Functor;
            return (other != null && this.name == other.name);
        }

        public override int GetHashCode()
        {
            return this.name.GetHashCode();
        }

        public override string ToString()
        {
            return this.name;
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
