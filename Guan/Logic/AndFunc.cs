///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.
//
// @File: AndFunc.cs
//
// @Owner: xunlu
// @Test:  xunlu
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
namespace Guan.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// And function.
    /// </summary>
    public class AndFunc : StandaloneFunc
    {
        public static readonly AndFunc Singleton = new AndFunc();

        private AndFunc()
            : base("and")
        {
        }

        public override object Invoke(object[] args)
        {
            foreach (object arg in args)
            {
                if (!(bool)arg)
                {
                    return false;
                }
            }

            return true;
        }

        internal override object Invoke(IPropertyContext context, List<GuanExpression> args)
        {
            foreach (GuanExpression exp in args)
            {
                object arg = exp.Evaluate(context);
                if ((arg == null) || !((bool)arg))
                {
                    return false;
                }
            }

            return true;
        }

        internal override GuanFunc Bind(List<GuanExpression> args)
        {
            for (int i = args.Count - 1; i >= 0; i--)
            {
                bool arg;
                if (args[i].GetLiteral(out arg))
                {
                    if (!arg)
                    {
                        return Literal.False;
                    }

                    args.RemoveAt(i);
                }
            }

            return this.Collapse(args);
        }
    }
}
