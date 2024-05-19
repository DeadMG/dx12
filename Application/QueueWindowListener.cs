using Renderer;

namespace Application
{
    public class QueueWindowListener : IWindowListener
    {
        private readonly InputQueue queue;

        public QueueWindowListener(InputQueue queue)
        {
            this.queue = queue;
        }

        public void OnResize(WindowSize size)
        {
            queue.Resize(size);
        }
    }
}
