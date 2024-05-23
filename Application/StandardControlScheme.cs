using Renderer;
using Simulation;
using System.Collections.Concurrent;
using System.Numerics;

namespace Application
{
    public class StandardControlScheme : IControlScheme
    {
        private readonly ConcurrentQueue<MouseWheelEvent> mouseWheelEvents = new ConcurrentQueue<MouseWheelEvent>();
        private readonly Player player;
        private volatile WindowSize viewSize;

        public StandardControlScheme(Player player, int width, int height)
        {
            this.player = player;
            viewSize = new WindowSize { Height = height, Width = width };
        }

        public float CameraSensitivity { get; set; } = 1 / (float)10;

        public void Apply(Game g)
        {
            var size = viewSize; // Atomic read

            while (mouseWheelEvents.TryDequeue(out var wheelEvent))
            {
                // The co-ordinate system we are expecting is
                // +x goes to right
                // +y goes down.
                // +amount is closer
                // The co-ordinate system we are transforming to is LH down, so from the user's perspective
                // +y is further away
                // +x is right
                // +z is up
                // So after projecting to scale
                // +y = -z 
                // +x = x
                // +amount = -y
                var camera = player.CameraFor(player.ViewingWorld(g));

                float x = ((wheelEvent.X / (float)size.Width) * 2) - 1;
                float z = ((wheelEvent.Y / (float)size.Height) * 2) - 1;

                var realLocation = Vector3.Normalize(new Vector3(x, -wheelEvent.Amount, -z));

                // Scale the amount depending on the camera Y
                camera.Position += realLocation * camera.Position.Y * CameraSensitivity;
            }
        }

        public void OnMouseWheel(float amount, int x, int y)
        {
            mouseWheelEvents.Enqueue(new MouseWheelEvent { Amount = amount, X = x, Y = y });
        }

        public void OnResize(int width, int height)
        {
            viewSize = new WindowSize { Height = height, Width = width };
        }

        private class MouseWheelEvent
        {
            public float Amount { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }
    }
}
