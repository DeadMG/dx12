namespace Application
{
    public class Latest<T>
        where T : class
    {
        private volatile T value;

        public void Set(T obj)
        {
            value = obj;
        }

        public bool Consume(out T? obj)
        {
            obj = Interlocked.Exchange(ref value, null);
            return obj != null;
        }

        public T? Read()
        {
            return value;
        }
    }
}
