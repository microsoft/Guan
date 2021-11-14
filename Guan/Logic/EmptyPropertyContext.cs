///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.
//
// @File: EmptyPropertyContext.cs
//
// @Owner: xunlu
// @Test:  xunlu
//
// Purpose:
//   Property context that does not have any property.
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Guan.Logic
{
    /// <summary>
    /// Property context that does not have any property.
    /// </summary>
    public sealed class EmptyPropertyContext : IPropertyContext
    {
        public static readonly EmptyPropertyContext Singleton = new EmptyPropertyContext();

        private EmptyPropertyContext()
        {
        }

        object IPropertyContext.this[string name]
        {
            get
            {
                return null;
            }
        }
    }
}
