using System.Runtime.InteropServices;

namespace Util
{
    public static class ListExtensions
    {
        public static uint SizeOf<T>(this IReadOnlyList<T> array)
            where T: unmanaged
        {
            return (uint)(Marshal.SizeOf<T>() * array.Count);
        }
    }
}
