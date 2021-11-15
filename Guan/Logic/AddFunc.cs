// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;

    internal class AddFunc : StandaloneFunc
    {
        public static readonly AddFunc Singleton = new AddFunc();

        private MathsFunc mathsAdd;

        private AddFunc()
            : base("add")
        {
            this.mathsAdd = new MathsFunc("add", '+');
        }

        public override object Invoke(object[] args)
        {
            if (args.Length == 2)
            {
                if (args[0] is DateTime)
                {
                    return this.AddDateTime((DateTime)args[0], args[1]);
                }

                object result = this.mathsAdd.TryInvoke(args);
                if (result != null)
                {
                    return result;
                }
            }

            string s = string.Empty;
            foreach (object arg in args)
            {
                if (arg != null)
                {
                    s += arg.ToString();
                }
            }

            return s;
        }

        private DateTime AddDateTime(DateTime arg1, object arg2)
        {
            if (arg2 is TimeSpan)
            {
                return arg1 + (TimeSpan)arg2;
            }

            if (arg2 is string)
            {
                return arg1 + TimeSpan.Parse((string)arg2);
            }

            throw new ArgumentException("Invalid argument to add to DateTime: " + arg2);
        }
    }
}
