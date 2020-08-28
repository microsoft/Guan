namespace Guan.Common
{
    public interface IPartialOrder
    {
        bool CompareTo(object other, out int result);
    }
}
