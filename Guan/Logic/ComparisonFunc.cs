// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;

    /// <summary>
    /// Comparison functions.
    /// If any of the parameter is a number (convertable to long),
    /// the comparison will be performed after converting both to long
    /// Otherwise string comparisons will be used.
    /// </summary>
    public class ComparisonFunc : BinaryFunc
    {
        public static readonly ComparisonFunc EQ = new ComparisonFunc("eq", Option.Equal);
        public static readonly ComparisonFunc NE = new ComparisonFunc("ne", Option.GreaterThan | Option.LessThan);
        public static readonly ComparisonFunc GT = new ComparisonFunc("gt", Option.GreaterThan);
        public static readonly ComparisonFunc LT = new ComparisonFunc("lt", Option.LessThan);
        public static readonly ComparisonFunc GE = new ComparisonFunc("ge", Option.GreaterThan | Option.Equal);
        public static readonly ComparisonFunc LE = new ComparisonFunc("le", Option.LessThan | Option.Equal);

        private readonly Option option;

        private ComparisonFunc(string name, Option option)
            : base(name)
        {
            this.option = option;
        }

        [Flags]
        private enum Option
        {
            LessThan = 1,
            Equal = 2,
            GreaterThan = 4,
        }

        public bool Invoke(object arg1, object arg2)
        {
            int result;
            ReleaseAssert.IsTrue(Utility.TryCompare(arg1, arg2, out result));
            return this.Compare(result);
        }

        public ComparisonFunc Inverse()
        {
            if (this == ComparisonFunc.LT)
            {
                return ComparisonFunc.GT;
            }
            else if (this == ComparisonFunc.GT)
            {
                return ComparisonFunc.LT;
            }
            else if (this == ComparisonFunc.LE)
            {
                return ComparisonFunc.GE;
            }
            else if (this == ComparisonFunc.GE)
            {
                return ComparisonFunc.LE;
            }

            return this;
        }

        internal bool Compare(int cmp)
        {
            if (cmp == 0)
            {
                return ((this.option & Option.Equal) != 0);
            }

            if (cmp < 0)
            {
                return ((this.option & Option.LessThan) != 0);
            }

            return ((this.option & Option.GreaterThan) != 0);
        }

        protected override object InvokeBinary(object arg1, object arg2)
        {
            int result;
            ReleaseAssert.IsTrue(
                Utility.TryCompare(arg1, arg2, out result),
                "Incompatible arguments to compare {0}/{1}:{2}/{3}",
                arg1,
                arg1 != null ? arg1.GetType() : null,
                arg2,
                arg2 != null ? arg2.GetType() : null);
            return this.Compare(result);
        }
    }
}
