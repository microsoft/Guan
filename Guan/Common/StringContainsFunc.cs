// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Guan.Common
{
    internal class StringContainsFunc : BinaryFunc
    {
        public static readonly StringContainsFunc Singleton = new StringContainsFunc();

        private StringContainsFunc()
            : base("StringContains")
        {
        }

        protected override object InvokeBinary(object arg1, object arg2)
        {
            if (arg1 == null || arg2 == null)
            {
                throw new GuanException("StringContains: Arguments cannot be null.");
            }

            if (arg1.GetType() != typeof(string) || arg2.GetType() != typeof(string))
            {
                throw new GuanException("StringContains: both required parameters must be of type System.String.");
            }

            string needle = (string)arg1;
            string haystack = (string)arg2;

            return haystack.Contains(needle);
        }
    }
}
