using System.Collections.Concurrent;

namespace Wrapper
{
    public class DisposeTracker : IDisposable
    {
        private readonly ConcurrentBag<IDisposable> disposables = new ConcurrentBag<IDisposable>();

        ~DisposeTracker()
        {
            Cleanup();
        }

        public T Track<T>(T item) where T : IDisposable
        {
            if (item == null) return item;
            disposables.Add(item);
            return item;
        }

        public void Dispose()
        {
            Cleanup();
            GC.SuppressFinalize(this);
        }

        private void Cleanup()
        {
            foreach (var item in disposables.Reverse())
            {
                item.Dispose();
            }
            disposables.Clear();
        }
    }
}
