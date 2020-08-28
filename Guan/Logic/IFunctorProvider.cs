// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Guan.Logic
{
    /// <summary>
    /// Interface providing functor (including predicate type) implementations.
    /// </summary>
    public interface IFunctorProvider
    {
        Functor FindFunctor(string name, Module from);
    }
}
