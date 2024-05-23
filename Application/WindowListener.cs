using Renderer;

namespace Application
{
    public class WindowListener : IWindowListener
    {
        private readonly ResizeTracker queue;
        private readonly IControlScheme scheme;

        public WindowListener(ResizeTracker queue, IControlScheme scheme)
        {
            this.queue = queue;
            this.scheme = scheme;
        }

        public void OnMouseWheel(float amount, int x, int y)
        {
            scheme.OnMouseWheel(amount, x, y);
        }

        public void OnResize(WindowSize size)
        {
            queue.Resize(size);
        }
    }
}
