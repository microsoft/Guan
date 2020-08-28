// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Guan.Common
{
    /// <summary>
    /// Property context that does not have any property.
    /// </summary>
    public class EmptyPropertyContext : IPropertyContext
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
