// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;

namespace Guan.Logic
{
    /// <summary>
    /// Container for functors.
    /// </summary>
    public class FunctorTable : IFunctorProvider
    {
        private IFunctorProvider provider_;
        private Dictionary<string, Functor> functors_;

        public FunctorTable(IFunctorProvider provider = null)
        {
            provider_ = provider;
            functors_ = new Dictionary<string, Functor>();
        }

        public void Add(Functor functor)
        {
            lock (functors_)
            {
                functors_.Add(functor.Name, functor);
            }
        }

        public Functor FindFunctor(string name, Module from)
        {
            lock (functors_)
            {
                Functor result;
                if (!functors_.TryGetValue(name, out result) && provider_ != null)
                {
                    result = provider_.FindFunctor(name, from);
                }

                return result;
            }
        }
    }
}
