///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.
//
// @File: BinaryFunc.cs
//
// @Owner: xunlu
// @Test:  xunlu
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
namespace Guan.Logic
{
    using System;

    /// <summary>
    /// Functions that take two arguments.
    /// </summary>
    public abstract class BinaryFunc : StandaloneFunc
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryFunc"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the function.</param>
        protected BinaryFunc(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Invoke the function.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>Function result.</returns>
        public override object Invoke(object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            if (args.Length != 2)
            {
                throw new ArgumentException("Invalid argument for BinaryFunc: " + this);
            }

            return this.InvokeBinary(args[0], args[1]);
        }

        /// <summary>
        /// Invoke the function with two arguments.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <returns>Function result.</returns>
        protected abstract object InvokeBinary(object arg1, object arg2);
    }
}
