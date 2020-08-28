// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Guan.Common
{
    /// <summary>
    /// Base exception generated from the trace tool.
    /// </summary>
    [Serializable]
    public class GuanException : Exception
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public GuanException()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">A message that describes the error.</param>
        public GuanException(string message, params object[] args)
            : base(string.Format(message, args))
        {
        }

        public GuanException(Exception inner, string message, params object[] args)
            : base(string.Format(message, args), inner)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source
        /// or destination.</param>
        protected GuanException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
