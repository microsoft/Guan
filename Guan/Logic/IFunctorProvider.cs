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
