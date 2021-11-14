///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.
//
// @File: GuanException.cs
//
// @Owner: xunlu
// @Test:  xunlu
//
// Purpose:
//   Define base exception generated from the trace tool.
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
namespace Guan.Logic
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Base exception generated from the trace tool.
    /// </summary>
    [Serializable]
    public class GuanException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GuanException"/> class.
        /// Default constructor.
        /// </summary>
        public GuanException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GuanException"/> class.
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
        /// Initializes a new instance of the <see cref="GuanException"/> class.
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
