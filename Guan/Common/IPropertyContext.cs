// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Guan.Common
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
        object this[string name]
        {
            set;
        }
    }
}
