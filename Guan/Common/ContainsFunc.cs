// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Guan.Common
{
    internal class ContainsFunc : BinaryFunc
    {
        public static readonly ContainsFunc Singleton = new ContainsFunc();

        private ContainsFunc()
            : base("Contains")
        {
        }

        protected override object InvokeBinary(object arg1, object arg2)
        {
            string needle = (string)arg1;
            string haystack = (string)arg2;

            return haystack.Contains(needle);
        }
    }
}
