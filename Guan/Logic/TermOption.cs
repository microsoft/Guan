// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Execution option for goal term.
    /// </summary>
    public class TermOption
    {
        public static readonly string MaxIteration = "max";
        public static readonly string Trace = "trace";
        public static readonly TermOption Default = new TermOption();

        private static readonly Functor OptionFunctor = new Functor("_option");

        private int max;
        private bool catchException;
        private List<string> traceTypes;
        private CompoundTerm term;

        public TermOption()
        {
            this.term = new CompoundTerm(OptionFunctor);
            this.catchException = false;
        }

        public TermOption(CompoundTerm term)
            : this()
        {
            if (!term.IsGround())
            {
                throw new ArgumentException("Option is not ground: " + term.ToString());
            }

            foreach (TermArgument arg in term.Arguments)
            {
                this[arg.Name] = term[arg.Name];
            }
        }

        public int Max
        {
            get
            {
                return this.max;
            }
        }

        public bool CatchException
        {
            get
            {
                return this.catchException;
            }
        }

        public object this[string name]
        {
            get
            {
                if (name == MaxIteration)
                {
                    return this.max;
                }

                return this.term[name];
            }

            internal set
            {
                if (name == MaxIteration)
                {
                    this.max = (int)(long)value;
                }
                else if (name == Trace)
                {
                    string traceConfig = (string)value;
                    this.traceTypes = new List<string>(traceConfig.Split(',', System.StringSplitOptions.RemoveEmptyEntries));
                }
                else if (name == "CatchException")
                {
                    this.catchException = Utility.Convert<bool>(value);
                }
                else
                {
                    this.term[name] = value;
                }
            }
        }

        internal (bool, bool) IsTraceEnabled(string type)
        {
            if (this.traceTypes != null)
            {
                foreach (string traceType in this.traceTypes)
                {
                    if (type == null || traceType.Contains(type))
                    {
                        return (true, traceType.StartsWith("!"));
                    }
                }
            }

            return (false, false);
        }
    }
}
