// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    internal class SplitFunc : BinaryFunc
    {
        public static readonly SplitFunc Singleton = new SplitFunc();

        private SplitFunc()
            : base("Split")
        {
        }

        protected override object InvokeBinary(object arg1, object arg2)
        {
            string input = (string)arg1;
            string delimiter = (string)arg2;
            char[] delimiters = new char[delimiter.Length];
            for (int i = 0; i < delimiter.Length; i++)
            {
                delimiters[i] = delimiter[i];
            }

            return input.Split(delimiters);
        }
    }
}
