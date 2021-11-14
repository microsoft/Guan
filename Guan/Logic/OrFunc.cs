// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Or function.
    /// </summary>
    public class OrFunc : StandaloneFunc
    {
        public static readonly OrFunc Singleton = new OrFunc();

        private OrFunc()
            : base("or")
        {
        }

        public override object Invoke(object[] args)
        {
            foreach (object arg in args)
            {
                if ((arg != null) && (bool)arg)
                {
                    return true;
                }
            }

            return false;
        }

        internal override object Invoke(IPropertyContext context, List<GuanExpression> args)
        {
            foreach (GuanExpression exp in args)
            {
                bool arg = (bool)exp.Evaluate(context);
                if (arg)
                {
                    return true;
                }
            }

            return false;
        }

        internal override GuanFunc Bind(List<GuanExpression> args)
        {
            for (int i = args.Count - 1; i >= 0; i--)
            {
                bool arg;
                if (args[i].GetLiteral(out arg))
                {
                    if (arg)
                    {
                        return Literal.True;
                    }

                    args.RemoveAt(i);
                }
            }

            return this.Collapse(args);
        }
    }
}
