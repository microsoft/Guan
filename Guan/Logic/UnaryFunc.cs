///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.
//
// @File: UnaryFunc.cs
//
// @Owner: xunlu
// @Test:  xunlu
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
namespace Guan.Logic
{
    using System;

    /// <summary>
    /// Functions that take only one argument.
    /// </summary>
    public abstract class UnaryFunc : StandaloneFunc
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnaryFunc"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the function.</param>
        protected UnaryFunc(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Invoke the function with one argument.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <returns>Function result.</returns>
        public abstract object UnaryInvoke(object arg);

        /// <summary>
        /// Invoke the function.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>Function result.</returns>
        public sealed override object Invoke(object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            if (args.Length != 1)
            {
                throw new ArgumentException("Invalid argument for UnaryFunc: " + this);
            }

            return this.UnaryInvoke(args[0]);
        }
    }
}
