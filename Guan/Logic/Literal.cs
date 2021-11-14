///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.
//
// @File: Literal.cs
//
// @Owner: xunlu
// @Test:  xunlu
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
namespace Guan.Logic
{
    /// <summary>
    /// Literal "function" which always returns the literal value itself.
    /// </summary>
    public class Literal : StandaloneFunc
    {
        internal static readonly Literal Empty = new Literal(null);
        internal static readonly Literal True = new Literal(true);
        internal static readonly Literal False = new Literal(false);

        private readonly object value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Literal"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="value">The value of the literal.</param>
        public Literal(object value)
            : base(value != null ? value.ToString() : string.Empty)
        {
            this.value = value;
        }

        public override object Invoke(object[] args)
        {
            return this.value;
        }
    }
}
