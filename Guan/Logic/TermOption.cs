// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Guan.Logic
{
    /// <summary>
    /// Execution option for goal term.
    /// </summary>
    public class TermOption
    {
        private int max_;
        private CompoundTerm term_;

        public static readonly string MaxIteration = "max";
        public static readonly TermOption Default = new TermOption();

        private static readonly Functor OptionFunctor = new Functor("_option");

        public TermOption()
            : this(new CompoundTerm(OptionFunctor))
        {
        }

        public TermOption(CompoundTerm term)
        {
            term_ = term;

            object maxValue = term_[MaxIteration];
            if (maxValue != null)
            {
                max_ = (int)(long)maxValue;
                term_.RemoveArgument(MaxIteration);
            }
        }

        public int Max
        {
            get
            {
                return max_;
            }
        }

        public object this[string name]
        {
            get
            {
                if (name == TermOption.MaxIteration)
                {
                    return max_;
                }

                return term_[name];
            }
            internal set
            {
                if (name == TermOption.MaxIteration)
                {
                    max_ = (int)value;
                }
                else
                {
                    term_[name] = value;
                }
            }
        }
    }
}
