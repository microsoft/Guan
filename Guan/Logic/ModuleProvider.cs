using System;
using System.Collections.Generic;
using Guan.Common;

namespace Guan.Logic
{
    /// <summary>
    /// Class maintaining predicate types from multiple modules to provide
    /// a lookup facility.
    /// </summary>
    internal class ModuleProvider : IFunctorProvider
    {
        /// <summary>
        /// Predicate types with the same name but different modules.
        /// </summary>
        class PredicateFunctorList : List<PredicateType>
        {
            public PredicateType Find(Module from)
            {
                if (Count == 1)
                {
                    return this[0];
                }

                List<PredicateType> result = new List<PredicateType>();
                int score = -1;
                foreach (PredicateType type in this)
                {
                    int newScore = GetScore(type, from);
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
                    throw new GuanException("Conflicting types {0} of module {1} and {2} referenced in {3}",
                        result[0].Name, result[0].Module, result[1].Module, from);
                }

                return result[0];
            }

            private int GetScore(PredicateType type, Module from)
            {
                string[] parts1 = type.Module.Name.Split('.');
                string[] parts2 = from.Name.Split('.');

                int i;
                for (i = 0; i < parts1.Length && i < parts2.Length && parts1[i] == parts2[i]; i++) ;

                return i;
            }
        }

        private Dictionary<string, PredicateFunctorList> types_;
        private List<IFunctorProvider> providers_;

        public ModuleProvider()
        {
            types_ = new Dictionary<string, PredicateFunctorList>();
            providers_ = new List<IFunctorProvider>();
        }

        public void Add(Module module)
        {
            foreach (PredicateType type in module.GetPublicTypes())
            {
                AddType(type);
            }
        }

        public void Add(IFunctorProvider provider)
        {
            if (!providers_.Contains(provider))
            {
                providers_.Add(provider);
            }
        }

        private void AddType(PredicateType type)
        {
            PredicateFunctorList entry;
            if (!types_.TryGetValue(type.Name, out entry))
            {
                entry = new PredicateFunctorList();
                types_.Add(type.Name, entry);
            }

            if (!entry.Contains(type))
            {
                entry.Add(type);
            }
        }

        public Functor FindFunctor(string name, Module from)
        {
            PredicateFunctorList entry;
            if (!types_.TryGetValue(name, out entry))
            {
                foreach (IFunctorProvider provider in providers_)
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
    }
}
