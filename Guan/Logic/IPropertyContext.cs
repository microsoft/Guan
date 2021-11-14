///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.
//
// @File: IPropertyContext.cs
//
// @Owner: xunlu
// @Test:  xunlu
//
// Purpose:
//   Define the interface for retrieving named property.
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Guan.Logic
{
    /// <summary>
    /// Interface implemented by classes that can expose named properties.
    /// The implementing class is free to define any property it wants
    /// to expose.  In the advanced scenario, the object can even define
    /// an infinite set of properties.
    /// </summary>
    public interface IPropertyContext
    {
        /// <summary>
        /// Property with a string name.
        /// </summary>
        /// <param name="name">Property name.</param>
        /// <returns>Property value, null if not found or the value
        /// is explicitly set to null.</returns>
        object this[string name]
        {
            get;
        }
    }

    public interface IWritablePropertyContext
    {
        /// <summary>
        /// Property with a string name.
        /// </summary>
        /// <param name="name">Property name.</param>
        /// <returns>Property value, null if not found or the value
        /// is explicitly set to null.</returns>
#pragma warning disable CA1044 // Properties should not be write only
        object this[string name]
#pragma warning restore CA1044 // Properties should not be write only
        {
            set;
        }
    }
}
