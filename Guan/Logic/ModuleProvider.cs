// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Class maintaining predicate types from multiple modules to provide
    /// a lookup facility.
    /// </summary>
    public class ModuleProvider : IFunctorProvider
    {
        private Dictionary<string, PredicateFunctorList> types;
        private List<IFunctorProvider> providers;

        public ModuleProvider()
        {
            this.types = new Dictionary<string, PredicateFunctorList>();
            this.providers = new List<IFunctorProvider>();
        }

        public void Add(Module module)
        {
            foreach (PredicateType type in module.GetPublicTypes())
            {
                this.AddType(type);
            }
        }

        public void Add(IFunctorProvider provider)
        {
            if (!this.providers.Contains(provider))
            {
                this.providers.Add(provider);
            }
        }

        public Functor FindFunctor(string name, Module from)
        {
            PredicateFunctorList entry;
            if (!this.types.TryGetValue(name, out entry))
            {
                foreach (IFunctorProvider provider in this.providers)
                {
                    Functor result = provider.FindFunctor(name, from);
                    if (result != null)
                    {
                        return result;
                    }
                }

                return null;
            }

            return entry.Find(from);
        }

        private void AddType(PredicateType type)
        {
            PredicateFunctorList entry;
            if (!this.types.TryGetValue(type.Name, out entry))
            {
                entry = new PredicateFunctorList();
                this.types.Add(type.Name, entry);
            }

            if (!entry.Contains(type))
            {
                entry.Add(type);
            }
        }

        /// <summary>
        /// Predicate types with the same name but different modules.
        /// </summary>
        private class PredicateFunctorList : List<PredicateType>
        {
            public PredicateType Find(Module from)
            {
                if (this.Count == 1)
                {
                    return this[0];
                }

                List<PredicateType> result = new List<PredicateType>();
                int score = -1;
                foreach (PredicateType type in this)
                {
                    int newScore = this.GetScore(type, from);
                    if (newScore > score)
                    {
                        result.Clear();
                        result.Add(type);
                        score = newScore;
                    }
                    else if (newScore == score)
                    {
                        result.Add(type);
                    }
                }

                if (result.Count == 0)
                {
                    return null;
                }

                if (result.Count > 1)
                {
                    throw new GuanException(
                        "Conflicting types {0} of module {1} and {2} referenced in {3}",
                        result[0].Name,
                        result[0].Module,
                        result[1].Module,
                        from);
                }

                return result[0];
            }

            private int GetScore(PredicateType type, Module from)
            {
                string[] parts1 = type.Module.Name.Split('.');
                string[] parts2 = from.Name.Split('.');

                int i;
                for (i = 0; i < parts1.Length && i < parts2.Length && parts1[i] == parts2[i]; i++)
                {
                }

                return i;
            }
        }
    }
}
