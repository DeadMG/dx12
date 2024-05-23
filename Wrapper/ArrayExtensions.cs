using System.Runtime.InteropServices;

namespace Wrapper
{
    public static class ArrayExtensions
    {
        public static int SizeOf<T>(this T[] array)
            where T: unmanaged
        {
            return Marshal.SizeOf<T>() * array.Length;
        }
    }
}
