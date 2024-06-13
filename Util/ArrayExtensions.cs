using System.Runtime.InteropServices;

namespace Util
{
    public static class ArrayExtensions
    {
        public static uint SizeOf<T>(this T[] array)
            where T: unmanaged
        {
            return (uint)(Marshal.SizeOf<T>() * array.Length);
        }

        public static Dispose<T> DisposeAll<T>(this T[] array)
            where T : IDisposable
        {
            return new Dispose<T>(array);
        }

        public class Dispose<T> : IDisposable
            where T : IDisposable
        {
            private readonly T[] array;

            public Dispose(T[] array)
            {
                this.array = array;
            }

            void IDisposable.Dispose()
            {
                foreach (var item in array)
                {
                    item.Dispose();
                }
            }

            public T[] Value => array;
        }
    }
}
