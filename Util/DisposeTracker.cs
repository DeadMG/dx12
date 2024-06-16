using System.Collections.Concurrent;

namespace Util
{
    public class DisposeTracker : IDisposable
    {
        private readonly ConcurrentBag<IDisposable> disposables = new ConcurrentBag<IDisposable>();

        public T Track<T>(T item) where T : IDisposable
        {
            disposables.Add(item);
            return item;
        }

        public void Dispose()
        {
            foreach (var item in disposables.Reverse())
            {
                item.Dispose();
            }
            disposables.Clear();
        }
    }
}
