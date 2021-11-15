// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    /// <summary>
    /// Not function.
    /// </summary>
    public class NotFunc : UnaryFunc
    {
        public static readonly NotFunc Singleton = new NotFunc();

        private NotFunc()
            : base("not")
        {
        }

        public override object UnaryInvoke(object arg)
        {
            return (arg == null || !(bool)arg);
        }
    }
}
