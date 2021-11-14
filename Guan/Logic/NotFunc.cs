///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.
//
// @File: NotFunc.cs
//
// @Owner: xunlu
// @Test:  xunlu
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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
