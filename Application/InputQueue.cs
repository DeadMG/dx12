using Renderer;

namespace Application
{
    public class InputQueue
    {
        // For resizes, track only latest.
        private WindowSize? resizeEvent;
        private readonly object resizeLock = new object();

        public void Resize(WindowSize resize)
        {
            lock (resizeLock)
            {
                resizeEvent = resize;
            }
        }

        public bool DidResize(out WindowSize? resize)
        {
            resize = null;
            lock (resizeLock)
            {
                if (resizeEvent == null) return false;
                resize = resizeEvent;
                resizeEvent = null;
                return true;
            }
        }
    }
}
