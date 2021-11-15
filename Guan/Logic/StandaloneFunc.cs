// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Functions that do not depend on the context.
    /// </summary>
    public abstract class StandaloneFunc : GuanFunc
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StandaloneFunc"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the function.</param>
        protected StandaloneFunc(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Invoke the function.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>Function result.</returns>
        public abstract object Invoke(object[] args);

        /// <summary>
        /// Invoke the function.
        /// </summary>
        /// <param name="context">The context class.</param>
        /// <param name="args">The array of arguments to the function.</param>
        /// <returns>The function result.</returns>
        public sealed override object Invoke(IPropertyContext context, object[] args)
        {
            return this.Invoke(args);
        }

        /// <summary>
        /// Replace the function with the only child argument.
        /// This is used during Bind to simplify the operation
        /// tree if the function result is the same as its only
        /// argument.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The optimized function object.</returns>
        internal GuanFunc Collapse(IList<GuanExpression> args)
        {
            if (args.Count == 1)
            {
                GuanExpression arg = args[0];

                // Replace arguments with the ones of the argument.
                args.Clear();
                foreach (GuanExpression child in arg.Children)
                {
                    args.Add(child);
                }

                return arg.Func;
            }

            return this;
        }
    }
}
