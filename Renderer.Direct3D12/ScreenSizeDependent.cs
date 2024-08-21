using Data.Space;

namespace Renderer.Direct3D12
{
    internal class ScreenSizeDependent<T> : IDisposable
        where T : IDisposable
    {
        private readonly Func<ScreenSize, T> create;
        private ScreenSize currentSize;
        private T item;

        public ScreenSizeDependent(ScreenSize starting, Func<ScreenSize, T> create)
        {
            this.currentSize = starting;
            this.create = create;

            item = create(starting);
        }

        public void Dispose() => item?.Dispose();

        public T GetFor(ScreenSize size)
        {
            if (currentSize != size)
            {
                item?.Dispose();
                currentSize = size;
                item = create(size);
            }

            return item;
        }
    }
}
