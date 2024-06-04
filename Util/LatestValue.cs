using System.Diagnostics.CodeAnalysis;

namespace Util
{
    public class LatestValue<T>
        where T : struct
    {
        private volatile Holder? holder;

        public void Set(T obj)
        {
            holder = new Holder { value = obj };
        }

        public bool TryConsume([NotNullWhen(true)] out T? value)
        {
            value = Interlocked.Exchange(ref holder, null)?.value;
            return value != null;
        }

        public T? Read()
        {
            return holder?.value;
        }

        private class Holder
        {
            public T value;
        }
    }
}
